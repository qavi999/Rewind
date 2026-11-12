using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ClipFlow {
    public class BufferEngine {
        private readonly AppSettings _settings;
        private Process _ffmpegProcess;
        private WasapiLoopbackServer _audioServer;
        private string _bufferDir;
        private bool _isRecording;
        private readonly object _lock = new object();
        private bool _isSaving;
        private string _resolvedFfmpegPath;
        private string _activeEncoder = "h264_nvenc";
        private bool _useDdagrab = true;
        private DateTime _recordStartTime = DateTime.MinValue;

        public event Action<bool, string> StatusChanged;
        public event Action<string, int> ClipSaved;
        public event Action<string> ErrorOccurred;

        public bool IsRecording {
            get { return _isRecording; }
        }

        public string ActiveEncoder {
            get { return _activeEncoder; }
        }

        public BufferEngine(AppSettings settings) {
            _settings = settings;
            InitBufferDir();
        }

        private void InitBufferDir() {
            try {
                string localData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rewind", "Buffer");
                if (!Directory.Exists(localData)) {
                    Directory.CreateDirectory(localData);
                }
                _bufferDir = localData;
            } catch {
                _bufferDir = Path.Combine(Path.GetTempPath(), "Rewind_Buffer");
                if (!Directory.Exists(_bufferDir)) {
                    Directory.CreateDirectory(_bufferDir);
                }
            }
        }

        public string FindOrExtractFfmpeg() {
            if (!string.IsNullOrEmpty(_resolvedFfmpegPath) && File.Exists(_resolvedFfmpegPath)) {
                return _resolvedFfmpegPath;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localAppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rewind", "bin");
            string localFfmpeg = Path.Combine(localAppDir, "ffmpeg.exe");
            string fbFfmpeg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flashback", "bin", "ffmpeg.exe");
            string legacyFfmpeg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipFlow", "bin", "ffmpeg.exe");

            string[] candidates = new string[] {
                Path.Combine(baseDir, "ffmpeg.exe"),
                localFfmpeg,
                fbFfmpeg,
                legacyFfmpeg,
                Path.Combine(baseDir, "..", "..", "tools", "ffmpeg.exe"),
                Path.Combine(baseDir, "tools", "ffmpeg.exe"),
                @"E:\clipowanie\tools\ffmpeg.exe"
            };

            foreach (var c in candidates) {
                try {
                    string full = Path.GetFullPath(c);
                    if (File.Exists(full) && new FileInfo(full).Length > 10000000) {
                        _resolvedFfmpegPath = full;
                        DetectEncoder(full);
                        return full;
                    }
                } catch { }
            }

            // Extract embedded compressed engine if missing
            try {
                if (!Directory.Exists(localAppDir)) {
                    Directory.CreateDirectory(localAppDir);
                }

                var asm = Assembly.GetExecutingAssembly();
                using (var resStream = asm.GetManifestResourceStream("ffmpeg.gz")) {
                    if (resStream != null) {
                        using (var gz = new GZipStream(resStream, CompressionMode.Decompress))
                        using (var fs = new FileStream(localFfmpeg, FileMode.Create, FileAccess.Write)) {
                            byte[] buffer = new byte[65536];
                            int read;
                            while ((read = gz.Read(buffer, 0, buffer.Length)) > 0) {
                                fs.Write(buffer, 0, read);
                            }
                        }

                        if (File.Exists(localFfmpeg) && new FileInfo(localFfmpeg).Length > 10000000) {
                            _resolvedFfmpegPath = localFfmpeg;
                            DetectEncoder(localFfmpeg);
                            return localFfmpeg;
                        }
                    }
                }
            } catch (Exception ex) {
                NotifyError("Błąd rozpakowywania silnika wideo: " + ex.Message);
            }

            _resolvedFfmpegPath = "ffmpeg.exe";
            return _resolvedFfmpegPath;
        }

        private void DetectEncoder(string ffmpegPath) {
            try {
                var psi = new ProcessStartInfo {
                    FileName = ffmpegPath,
                    Arguments = "-hide_banner -encoders",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi)) {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);

                    if (output.Contains("h264_nvenc")) {
                        _activeEncoder = "h264_nvenc"; // NVIDIA GeForce RTX
                    } else if (output.Contains("h264_amf")) {
                        _activeEncoder = "h264_amf"; // AMD Radeon
                    } else if (output.Contains("h264_qsv")) {
                        _activeEncoder = "h264_qsv"; // Intel QuickSync
                    } else {
                        _activeEncoder = "libx264"; // Universal CPU fallback
                    }
                }
            } catch {
                _activeEncoder = "h264_nvenc";
            }
        }

        public void Start() {
            lock (_lock) {
                if (_isRecording) return;
                CleanBufferDir();

                string ffmpegPath = FindOrExtractFfmpeg();
                if (!File.Exists(ffmpegPath)) {
                    NotifyStatus(false, "Brak silnika FFmpeg");
                    return;
                }

                // Start audio loopback server
                try {
                    if (_audioServer != null) {
                        _audioServer.Stop();
                        _audioServer.Dispose();
                    }
                    _audioServer = new WasapiLoopbackServer(
                        _settings.SystemVolumePercent / 100f,
                        _settings.MicVolumePercent / 100f,
                        _settings.SelectedMicId
                    );
                    _audioServer.Start();
                } catch (Exception ex) {
                    NotifyError("Ostrzeżenie audio: " + ex.Message);
                }

                int segTime = 1;
                int duration = Math.Max(15, _settings.ClipDuration);
                int segCount = (int)Math.Ceiling(duration / (double)segTime);
                int wrapCount = Math.Max(60, segCount * 2 + 10);

                int fps = _settings.Fps > 0 ? _settings.Fps : 60;
                string bitrate = string.IsNullOrEmpty(_settings.Bitrate) ? "12M" : _settings.Bitrate;
                string segPattern = Path.Combine(_bufferDir, "seg_%02d.ts");
                int drawMouse = (_settings != null && _settings.CaptureCursor) ? 1 : 0;

                string audioInput = "";
                string mapAudio = "";
                string audioCodec = "";
                if (_audioServer != null) {
                    audioInput = string.Format("-f f32le -ar {0} -ac 2 -i \"{1}\"", _audioServer.SampleRate, _audioServer.PipePath);
                    mapAudio = "-map 0:a";
                    audioCodec = "-c:a aac -b:a 192k";
                }

                string args;

                if (_useDdagrab) {
                    int monitorIndex = 0;
                    try {
                        var screens = Screen.AllScreens;
                        if (screens != null && screens.Length > 0) {
                            if (!string.IsNullOrEmpty(_settings.SelectedDisplayDevice)) {
                                for (int i = 0; i < screens.Length; i++) {
                                    if (string.Equals(screens[i].DeviceName, _settings.SelectedDisplayDevice, StringComparison.OrdinalIgnoreCase)) {
                                        monitorIndex = i;
                                        break;
                                    }
                                }
                            } else if (_settings.SelectedDisplayIndex >= 0 && _settings.SelectedDisplayIndex < screens.Length) {
                                monitorIndex = _settings.SelectedDisplayIndex;
                            }
                        }
                    } catch { }

                    string ddaFilter = string.Format("ddagrab=framerate={0}:output_idx={1}:draw_mouse={2}", fps, monitorIndex, drawMouse);
                    string encoderParams;

                    if (_activeEncoder == "h264_nvenc") {
                        encoderParams = string.Format("-c:v h264_nvenc -preset p1 -tune ull -zerolatency 1 -b:v {0} -maxrate 16M -bufsize 8M", bitrate);
                    } else if (_activeEncoder == "h264_amf") {
                        encoderParams = string.Format("-c:v h264_amf -quality speed -b:v {0}", bitrate);
                    } else if (_activeEncoder == "h264_qsv") {
                        ddaFilter += ",hwdownload,format=bgra";
                        encoderParams = string.Format("-c:v h264_qsv -preset veryfast -b:v {0} -pix_fmt nv12", bitrate);
                    } else {
                        ddaFilter += ",hwdownload,format=bgra";
                        encoderParams = string.Format("-c:v libx264 -preset ultrafast -tune zerolatency -b:v {0} -pix_fmt yuv420p", bitrate);
                    }

                    args = string.Format(
                        "-y -nostats -loglevel error -init_hw_device d3d11va -filter_complex \"{0}[v]\" {1} " +
                        "-map \"[v]\" {2} {3} {4} " +
                        "-g {5} -keyint_min {5} -f segment -segment_time {6} -segment_wrap {7} -reset_timestamps 1 \"{8}\"",
                        ddaFilter, audioInput, mapAudio, encoderParams, audioCodec, fps, segTime, wrapCount, segPattern
                    );
                } else {
                    string encoderParams;
                    if (_activeEncoder == "h264_nvenc") {
                        encoderParams = string.Format("-c:v h264_nvenc -preset p1 -tune ull -zerolatency 1 -b:v {0} -maxrate 16M -bufsize 8M -pix_fmt yuv420p", bitrate);
                    } else if (_activeEncoder == "h264_amf") {
                        encoderParams = string.Format("-c:v h264_amf -quality speed -b:v {0} -pix_fmt yuv420p", bitrate);
                    } else if (_activeEncoder == "h264_qsv") {
                        encoderParams = string.Format("-c:v h264_qsv -preset veryfast -b:v {0} -pix_fmt yuv420p", bitrate);
                    } else {
                        encoderParams = string.Format("-c:v libx264 -preset ultrafast -tune zerolatency -b:v {0} -pix_fmt yuv420p", bitrate);
                    }

                    string screenArgs = "";
                    try {
                        var screens = Screen.AllScreens;
                        if (screens != null && screens.Length > 0) {
                            Screen targetScreen = null;
                            if (!string.IsNullOrEmpty(_settings.SelectedDisplayDevice)) {
                                targetScreen = screens.FirstOrDefault(s => string.Equals(s.DeviceName, _settings.SelectedDisplayDevice, StringComparison.OrdinalIgnoreCase));
                            }
                            if (targetScreen == null && _settings.SelectedDisplayIndex >= 0 && _settings.SelectedDisplayIndex < screens.Length) {
                                targetScreen = screens[_settings.SelectedDisplayIndex];
                            }
                            if (targetScreen == null) {
                                targetScreen = screens.FirstOrDefault(s => s.Primary) ?? screens[0];
                            }
                            if (targetScreen != null) {
                                screenArgs = string.Format("-offset_x {0} -offset_y {1} -video_size {2}x{3} ",
                                    targetScreen.Bounds.X,
                                    targetScreen.Bounds.Y,
                                    targetScreen.Bounds.Width,
                                    targetScreen.Bounds.Height);
                            }
                        }
                    } catch { }

                    args = string.Format(
                        "-y -nostats -loglevel error -f gdigrab -framerate {0} -rtbufsize 256M -draw_mouse {1} {2}-i desktop " +
                        "{3} " +
                        "{4} " +
                        "{5} " +
                        "-g {0} -keyint_min {0} -f segment -segment_time {6} -segment_wrap {7} -reset_timestamps 1 \"{8}\"",
                        fps, drawMouse, screenArgs, audioInput, encoderParams, audioCodec, segTime, wrapCount, segPattern
                    );
                }

                try {
                    var psi = new ProcessStartInfo {
                        FileName = ffmpegPath,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    _ffmpegProcess = new Process {
                        StartInfo = psi,
                        EnableRaisingEvents = true
                    };

                    _ffmpegProcess.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                        // Consumes stderr asynchronously so FFmpeg pipe never deadlocks
                    };

                    _ffmpegProcess.Exited += OnProcessExited;
                    _recordStartTime = DateTime.UtcNow;
                    _ffmpegProcess.Start();
                    _ffmpegProcess.BeginErrorReadLine();

                    try {
                        _ffmpegProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                    } catch { }

                    _isRecording = true;
                    string encoderLabel = _activeEncoder.Replace("h264_", "").ToUpperInvariant();
                    NotifyStatus(true, string.Format("Bufor aktywny ({0} + Audio)", encoderLabel));
                } catch (Exception ex) {
                    _isRecording = false;
                    NotifyError("Błąd uruchamiania bufora: " + ex.Message);
                    NotifyStatus(false, "Błąd bufora");
                }
            }
        }

        private void OnProcessExited(object sender, EventArgs e) {
            lock (_lock) {
                if (_isRecording) {
                    _isRecording = false;

                    if (_useDdagrab && (DateTime.UtcNow - _recordStartTime).TotalSeconds < 3.0) {
                        _useDdagrab = false;
                        NotifyStatus(false, "Przełączanie na tryb GDI...");
                        ThreadPool.QueueUserWorkItem(delegate {
                            Thread.Sleep(300);
                            Start();
                        });
                        return;
                    }

                    NotifyStatus(false, "Bufor zatrzymany");
                }
            }
        }

        public void UpdateAudioVolumes(int sysPercent, int micPercent) {
            if (_audioServer != null) {
                _audioServer.SystemVolume = sysPercent / 100f;
                _audioServer.MicVolume = micPercent / 100f;
            }
        }

        public void Stop() {
            lock (_lock) {
                if (!_isRecording && _ffmpegProcess == null) return;
                _isRecording = false;

                try {
                    if (_ffmpegProcess != null) {
                        _ffmpegProcess.Exited -= OnProcessExited;
                        if (!_ffmpegProcess.HasExited) {
                            try {
                                _ffmpegProcess.StandardInput.WriteLine("q");
                                if (!_ffmpegProcess.WaitForExit(1500)) {
                                    _ffmpegProcess.Kill();
                                }
                            } catch {
                                _ffmpegProcess.Kill();
                            }
                        }
                    }
                } catch { } finally {
                    _ffmpegProcess = null;
                }

                try {
                    if (_audioServer != null) {
                        _audioServer.Stop();
                        _audioServer.Dispose();
                        _audioServer = null;
                    }
                } catch { }

                NotifyStatus(false, "Bufor zatrzymany");
                CleanBufferDir();
            }
        }

        public void SaveClip() {
            if (_isSaving) return;

            // Instant audio feedback
            if (_settings.PlaySound) {
                SoundEffect.Play();
            }

            ThreadPool.QueueUserWorkItem(delegate {
                lock (_lock) {
                    if (_isSaving) return;
                    _isSaving = true;
                }

                try {
                    // Wait for the active segment boundary to complete and flush all frames to disk.
                    // This guarantees zero missing frames and captures the full play plus aftermath.
                    Thread.Sleep(1400);
                    ExecuteSaveClip();
                } catch (Exception ex) {
                    NotifyError("Błąd zapisu klipu: " + ex.Message);
                } finally {
                    lock (_lock) {
                        _isSaving = false;
                    }
                }
            });
        }

        private void ExecuteSaveClip() {
            if (!Directory.Exists(_bufferDir)) {
                NotifyError("Katalog bufora nie istnieje.");
                return;
            }

            var dir = new DirectoryInfo(_bufferDir);
            var files = dir.GetFiles("seg_*.ts")
                           .Where(f => f.Length > 2048)
                           .OrderByDescending(f => f.LastWriteTimeUtc)
                           .ToList();

            if (files.Count == 0) {
                NotifyError("Brak nagranych danych w buforze (odczekaj kilka sekund po włączeniu).");
                return;
            }

            int segTime = 1;
            int duration = Math.Max(15, _settings.ClipDuration);
            int neededCount = (int)Math.Ceiling(duration / (double)segTime) + 1;

            var chosen = new List<string>();
            string snapFile = null;

            // files[0] is the currently active/latest segment capturing up to the moment F8 was pressed.
            // Create a non-locking snapshot using FileShare.ReadWrite so concat runs smoothly without write contention.
            try {
                if (files.Count > 0 && files[0].Length > 4096) {
                    snapFile = Path.Combine(_bufferDir, "snap_" + Guid.NewGuid().ToString("N") + ".ts");
                    CopySnapshot(files[0].FullName, snapFile);
                    chosen.Add(snapFile);
                }
            } catch {
                if (files.Count > 0) {
                    chosen.Add(files[0].FullName);
                }
            }

            // Append previous completed segments to satisfy requested clip duration
            for (int i = 1; i < files.Count && chosen.Count < neededCount; i++) {
                chosen.Add(files[i].FullName);
            }

            if (chosen.Count == 0) {
                NotifyError("Niewystarczająca ilość danych w buforze.");
                return;
            }

            // Reverse from newest-first to oldest-first (chronological order)
            chosen.Reverse();

            string listFile = Path.Combine(_bufferDir, "concat_" + Guid.NewGuid().ToString("N") + ".txt");
            var sb = new StringBuilder();
            foreach (var f in chosen) {
                sb.AppendLine(string.Format("file '{0}'", f.Replace("\\", "/")));
            }
            File.WriteAllText(listFile, sb.ToString());

            string clipsDir = _settings.ClipsFolder;
            if (string.IsNullOrEmpty(clipsDir)) {
                clipsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clips");
            }
            if (!Directory.Exists(clipsDir)) {
                Directory.CreateDirectory(clipsDir);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string outputClip = Path.Combine(clipsDir, string.Format("Clip_{0}.mp4", timestamp));
            string ffmpegPath = FindOrExtractFfmpeg();

            // Instant stream copy concatenation for both video & audio (< 150ms)
            string concatArgs = string.Format(
                "-y -nostats -loglevel error -f concat -safe 0 -i \"{0}\" -c copy -movflags +faststart \"{1}\"",
                listFile, outputClip
            );

            var psi = new ProcessStartInfo {
                FileName = ffmpegPath,
                Arguments = concatArgs,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var proc = Process.Start(psi)) {
                proc.WaitForExit(10000);
            }

            try { File.Delete(listFile); } catch { }
            if (snapFile != null && File.Exists(snapFile)) {
                try { File.Delete(snapFile); } catch { }
            }

            if (File.Exists(outputClip) && new FileInfo(outputClip).Length > 0) {
                int actualDuration = chosen.Count * segTime;
                if (ClipSaved != null) {
                    ClipSaved(outputClip, actualDuration);
                }
            } else {
                NotifyError("Nie udało się wygenerować pliku MP4 klipu.");
            }
        }

        private static void CopySnapshot(string sourcePath, string destPath) {
            using (var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write)) {
                byte[] buffer = new byte[65536];
                int bytesRead;
                while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0) {
                    dst.Write(buffer, 0, bytesRead);
                }
            }
        }

        private void CleanBufferDir() {
            try {
                if (Directory.Exists(_bufferDir)) {
                    var dir = new DirectoryInfo(_bufferDir);
                    foreach (var f in dir.GetFiles()) {
                        try { f.Delete(); } catch { }
                    }
                }
            } catch { }
        }

        private void NotifyStatus(bool recording, string text) {
            if (StatusChanged != null) {
                StatusChanged(recording, text);
            }
        }

        private void NotifyError(string error) {
            if (ErrorOccurred != null) {
                ErrorOccurred(error);
            }
        }
    }
}
