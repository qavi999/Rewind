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
        private System.Threading.Timer _watchdogTimer;
        private volatile bool _isSessionLocked = false;
        private volatile bool _isStopping = false;
        private int _consecutiveCrashes = 0;

        public event Action<bool, string> StatusChanged;
        public event Action<int> ClipSaving;
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
            _watchdogTimer = new System.Threading.Timer(WatchdogCallback, null, 3000, 2500);
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

        private void KillStaleFfmpegProcesses() {
            try {
                foreach (var p in Process.GetProcessesByName("ffmpeg")) {
                    try {
                        if (_ffmpegProcess != null && p.Id == _ffmpegProcess.Id) continue;
                        bool isOurs = false;
                        try {
                            string path = p.MainModule != null ? p.MainModule.FileName : "";
                            if (!string.IsNullOrEmpty(path)) {
                                if (path.IndexOf("Rewind", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    path.IndexOf("clipowanie", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    path.IndexOf("Flashback", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    path.IndexOf("ClipFlow", StringComparison.OrdinalIgnoreCase) >= 0) {
                                    isOurs = true;
                                }
                            }
                        } catch { }

                        if (isOurs) {
                            p.Kill();
                        }
                    } catch { }
                }
            } catch { }
        }

        public void Start() {
            lock (_lock) {
                if (_isRecording || _isSessionLocked) return;
                KillStaleFfmpegProcesses();
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
                        _settings.SelectedMicId,
                        _settings.SeparateAudioTracks
                    );
                    _audioServer.Start();
                } catch (Exception ex) {
                    NotifyError("Ostrzeżenie audio: " + ex.Message);
                }

                int segTime = 2;
                int fps = _settings.Fps > 0 ? _settings.Fps : 60;
                string bitrate = string.IsNullOrEmpty(_settings.Bitrate) ? "12M" : _settings.Bitrate;
                int bitMb = 12;
                string bitClean = bitrate.Trim().ToUpperInvariant();
                if (bitClean.EndsWith("M")) {
                    int.TryParse(bitClean.Substring(0, bitClean.Length - 1), out bitMb);
                } else if (int.TryParse(bitClean, out bitMb)) {
                    bitrate = bitMb + "M";
                }
                if (bitMb <= 0) bitMb = 12;
                string maxrate = (bitMb + 4) + "M";
                string bufsize = Math.Max(4, bitMb / 2) + "M";

                string segPattern = Path.Combine(_bufferDir, "seg_%06d.ts");
                int drawMouse = (_settings != null && _settings.CaptureCursor) ? 1 : 0;

                string audioInput = "";
                string mapAudio = "";
                string audioCodec = "";
                if (_audioServer != null) {
                    audioInput = string.Format("-f f32le -ar {0} -ac {1} -i \"{2}\"", _audioServer.SampleRate, _audioServer.OutputChannels, _audioServer.PipePath);
                    if (_settings.SeparateAudioTracks) {
                        mapAudio = "-map \"[a_sys]\" -map \"[a_mic]\"";
                        audioCodec = "-c:a aac -b:a 192k -metadata:s:a:0 title=\"Dźwięk gry\" -metadata:s:a:1 title=\"Mikrofon\"";
                    } else {
                        mapAudio = "-map 0:a";
                        audioCodec = "-c:a aac -b:a 192k";
                    }
                }

                string args;

                bool useDda = _useDdagrab && !string.Equals(_settings.CaptureEngine, "GDI", StringComparison.OrdinalIgnoreCase);

                if (useDda) {
                    int monitorIndex = 0;
                    string targetDeviceName = null;
                    try {
                        var screens = Screen.AllScreens;
                        if (screens != null && screens.Length > 0) {
                            Screen targetScreen = null;
                            if (!string.IsNullOrEmpty(_settings.SelectedDisplayDevice)) {
                                for (int i = 0; i < screens.Length; i++) {
                                    if (string.Equals(screens[i].DeviceName, _settings.SelectedDisplayDevice, StringComparison.OrdinalIgnoreCase)) {
                                        monitorIndex = i;
                                        targetScreen = screens[i];
                                        break;
                                    }
                                }
                            } else if (_settings.SelectedDisplayIndex >= 0 && _settings.SelectedDisplayIndex < screens.Length) {
                                monitorIndex = _settings.SelectedDisplayIndex;
                                targetScreen = screens[monitorIndex];
                            }
                            if (targetScreen == null) {
                                targetScreen = screens.FirstOrDefault(s => s.Primary) ?? screens[0];
                            }
                            if (targetScreen != null) {
                                targetDeviceName = targetScreen.DeviceName;
                            }
                        }
                    } catch { }

                    // Query native hardware monitor refresh rate (e.g. 144Hz, 165Hz, 240Hz)
                    int nativeHz = NativeMethods.GetMonitorRefreshRate(targetDeviceName);
                    if (nativeHz < 30) nativeHz = 60;

                    // Match Desktop Duplication polling to native Hz so G-Sync/DWM never drops monitor Hz!
                    int captureRate = Math.Max(nativeHz, fps);

                    string ddaFilter = string.Format("ddagrab=framerate={0}:output_idx={1}:draw_mouse={2}", captureRate, monitorIndex, drawMouse);
                    if (captureRate > fps) {
                        ddaFilter += string.Format(",fps={0}", fps);
                    }
                    string encoderParams;

                    if (_activeEncoder == "h264_nvenc") {
                        encoderParams = string.Format("-c:v h264_nvenc -preset p1 -tune ull -zerolatency 1 -b:v {0} -maxrate {1} -bufsize {2}", bitrate, maxrate, bufsize);
                    } else if (_activeEncoder == "h264_amf") {
                        encoderParams = string.Format("-c:v h264_amf -quality speed -b:v {0}", bitrate);
                    } else if (_activeEncoder == "h264_qsv") {
                        ddaFilter += ",hwdownload,format=bgra";
                        encoderParams = string.Format("-c:v h264_qsv -preset veryfast -b:v {0} -pix_fmt nv12", bitrate);
                    } else {
                        ddaFilter += ",hwdownload,format=bgra";
                        encoderParams = string.Format("-c:v libx264 -preset ultrafast -tune zerolatency -b:v {0} -pix_fmt yuv420p", bitrate);
                    }

                    string fullFilterComplex = string.Format("\"{0}[v]\"", ddaFilter);
                    if (_settings.SeparateAudioTracks && _audioServer != null) {
                        fullFilterComplex = string.Format("\"{0}[v];[0:a]pan=stereo|c0=c0|c1=c1[a_sys];[0:a]pan=stereo|c0=c2|c1=c3[a_mic]\"", ddaFilter);
                    }

                    args = string.Format(
                        "-y -nostats -loglevel error -init_hw_device d3d11va -filter_complex {0} {1} " +
                        "-map \"[v]\" {2} {3} {4} " +
                        "-g {5} -keyint_min {5} -f segment -segment_time {6} -segment_start_number 1 -reset_timestamps 0 \"{7}\"",
                        fullFilterComplex, audioInput, mapAudio, encoderParams, audioCodec, fps, segTime, segPattern
                    );
                } else {
                    string encoderParams;
                    if (_activeEncoder == "h264_nvenc") {
                        encoderParams = string.Format("-c:v h264_nvenc -preset p1 -tune ull -zerolatency 1 -b:v {0} -maxrate {1} -bufsize {2} -pix_fmt yuv420p", bitrate, maxrate, bufsize);
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

                    string gdiFilter = "";
                    string gdiMap = "";
                    if (_settings.SeparateAudioTracks && _audioServer != null) {
                        gdiFilter = "-filter_complex \"[1:a]pan=stereo|c0=c0|c1=c1[a_sys];[1:a]pan=stereo|c0=c2|c1=c3[a_mic]\" ";
                        gdiMap = "-map 0:v -map \"[a_sys]\" -map \"[a_mic]\" ";
                    }

                    args = string.Format(
                        "-y -nostats -loglevel error -f gdigrab -framerate {0} -rtbufsize 256M -draw_mouse {1} {2}-i desktop " +
                        "{3} {4}{5} " +
                        "{6} " +
                        "{7} " +
                        "-g {0} -keyint_min {0} -f segment -segment_time {8} -segment_start_number 1 -reset_timestamps 0 \"{9}\"",
                        fps, drawMouse, screenArgs, audioInput, gdiFilter, gdiMap, encoderParams, audioCodec, segTime, segPattern
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
                    ChildProcessTracker.AddProcess(_ffmpegProcess);
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
                if (_isStopping || _isSessionLocked) return;

                if (_isRecording) {
                    _isRecording = false;

                    if (_useDdagrab && (DateTime.UtcNow - _recordStartTime).TotalSeconds < 3.0) {
                        _consecutiveCrashes++;
                        if (_consecutiveCrashes >= 2) {
                            _useDdagrab = false;
                            NotifyStatus(false, "Przełączanie na tryb GDI...");
                        }
                    } else {
                        _consecutiveCrashes = 0;
                    }

                    NotifyStatus(false, "Bufor zatrzymany (auto-wznawianie...)");

                    ThreadPool.QueueUserWorkItem(delegate {
                        Thread.Sleep(1500);
                        lock (_lock) {
                            if (!_isSessionLocked && _settings.BufferActive && !_isRecording) {
                                Start();
                            }
                        }
                    });
                }
            }
        }

        public void OnSessionLock() {
            lock (_lock) {
                _isSessionLocked = true;
                StopInternal(cleanBuffer: true);
                NotifyStatus(false, "Zablokowany ekran Windows (bufor wstrzymany)");
            }
        }

        public void OnSessionUnlock() {
            ThreadPool.QueueUserWorkItem(delegate {
                Thread.Sleep(1200); // Allow display and audio session to restore
                lock (_lock) {
                    _isSessionLocked = false;
                    _useDdagrab = true; // Always restore DXGI on unlock
                    _consecutiveCrashes = 0;
                    if (_settings.BufferActive && !_isRecording) {
                        Start();
                    }
                }
            });
        }

        public void OnDisplayChanged() {
            ThreadPool.QueueUserWorkItem(delegate {
                Thread.Sleep(1000);
                lock (_lock) {
                    if (!_isSessionLocked && _settings.BufferActive) {
                        StopInternal(cleanBuffer: true);
                        Start();
                    }
                }
            });
        }

        private void WatchdogCallback(object state) {
            try {
                lock (_lock) {
                    if (!_settings.BufferActive || _isSessionLocked || _isStopping || _isSaving) {
                        return;
                    }

                    // Revive if process died
                    if (_ffmpegProcess == null || _ffmpegProcess.HasExited) {
                        StopInternal(cleanBuffer: true);
                        Start();
                        return;
                    }

                    // Check for stalled FFmpeg process
                    if (_isRecording && (DateTime.UtcNow - _recordStartTime).TotalSeconds > 8.0) {
                        if (Directory.Exists(_bufferDir)) {
                            var dir = new DirectoryInfo(_bufferDir);
                            var files = dir.GetFiles("seg_*.ts");
                            if (files.Length > 0) {
                                var newest = files.OrderByDescending(f => f.LastWriteTimeUtc).First();
                                if ((DateTime.UtcNow - newest.LastWriteTimeUtc).TotalSeconds > 10.0) {
                                    try { _ffmpegProcess.Kill(); } catch { }
                                    StopInternal(cleanBuffer: true);
                                    Start();
                                    return;
                                }
                            }
                        }
                    }

                    TrimRingBuffer();
                }
            } catch { }
        }

        private void TrimRingBuffer() {
            try {
                if (!Directory.Exists(_bufferDir)) return;
                var dir = new DirectoryInfo(_bufferDir);
                var files = dir.GetFiles("seg_*.ts");
                if (files.Length == 0) return;

                int duration = Math.Max(5, _settings.ClipDuration);
                if (_settings.SecondaryHotkeyEnabled) {
                    duration = Math.Max(duration, Math.Max(5, _settings.SecondaryClipDuration));
                }
                int neededCount = (int)Math.Ceiling(duration / 2.0) + 1;
                int keepCount = neededCount + 6; // Safety margin: keep neededCount + 12 seconds

                var segments = new List<Tuple<int, FileInfo>>();
                foreach (var f in files) {
                    int idx = ParseSegmentIndex(f.Name);
                    if (idx > 0) {
                        segments.Add(new Tuple<int, FileInfo>(idx, f));
                    }
                }

                if (segments.Count <= keepCount) return;

                segments.Sort(delegate(Tuple<int, FileInfo> a, Tuple<int, FileInfo> b) {
                    return a.Item1.CompareTo(b.Item1);
                });

                int maxIdx = segments[segments.Count - 1].Item1;
                int cutoffIdx = maxIdx - keepCount;

                foreach (var s in segments) {
                    if (s.Item1 < cutoffIdx) {
                        try { s.Item2.Delete(); } catch { }
                    }
                }
            } catch { }
        }

        public static int ParseSegmentIndex(string fileName) {
            try {
                string name = Path.GetFileNameWithoutExtension(fileName);
                if (name != null && name.StartsWith("seg_")) {
                    int val;
                    if (int.TryParse(name.Substring(4), out val)) {
                        return val;
                    }
                }
            } catch { }
            return -1;
        }

        public void UpdateAudioVolumes(int sysPercent, int micPercent) {
            if (_audioServer != null) {
                _audioServer.SystemVolume = sysPercent / 100f;
                _audioServer.MicVolume = micPercent / 100f;
            }
        }

        public void Stop() {
            lock (_lock) {
                _isStopping = true;
                StopInternal(cleanBuffer: true);
                NotifyStatus(false, "Bufor zatrzymany");
                _isStopping = false;
            }
        }

        private void StopInternal(bool cleanBuffer) {
            _isRecording = false;

            try {
                if (_ffmpegProcess != null) {
                    _ffmpegProcess.Exited -= OnProcessExited;
                    if (!_ffmpegProcess.HasExited) {
                        try {
                            _ffmpegProcess.StandardInput.WriteLine("q");
                            if (!_ffmpegProcess.WaitForExit(1000)) {
                                _ffmpegProcess.Kill();
                            }
                        } catch {
                            try { _ffmpegProcess.Kill(); } catch { }
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

            if (cleanBuffer) {
                CleanBufferDir();
            }
        }

        public void SaveClip(int targetDuration = 0) {
            lock (_lock) {
                if (_isSaving) return;
                if (!_isRecording) {
                    NotifyError("Bufor nie jest aktywny (trwa wznawianie). Spróbuj za chwilę.");
                    if (_settings.BufferActive && !_isSessionLocked) {
                        ThreadPool.QueueUserWorkItem(delegate { Start(); });
                    }
                    return;
                }
                _isSaving = true;
            }

            int dur = targetDuration > 0 ? targetDuration : _settings.ClipDuration;

            // 1. Natychmiastowe potwierdzenie dźwiękowe w momencie kliknięcia / skrótu (0 ms latencji)
            if (_settings.PlaySound) {
                SoundEffect.Play();
            }

            // 2. Natychmiastowe powiadomienie UI o rozpoczęciu zapisu powtórki
            if (ClipSaving != null) {
                try { ClipSaving(dur); } catch { }
            }

            // 3. Natychmiastowy zapis w tle bez sztucznego opóźnienia
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    ExecuteSaveClip(dur);
                } catch (Exception ex) {
                    NotifyError("Błąd zapisu klipu: " + ex.Message);
                } finally {
                    lock (_lock) {
                        _isSaving = false;
                    }
                }
            });
        }

        private void ExecuteSaveClip(int targetDuration) {
            if (!Directory.Exists(_bufferDir)) {
                NotifyError("Katalog bufora nie istnieje.");
                return;
            }

            var dir = new DirectoryInfo(_bufferDir);
            var files = dir.GetFiles("seg_*.ts")
                           .Where(f => f.Length > 2048 && ParseSegmentIndex(f.Name) > 0)
                           .OrderBy(f => ParseSegmentIndex(f.Name))
                           .ToList();

            if (files.Count == 0) {
                NotifyError("Brak nagranych danych w buforze (odczekaj kilka sekund po włączeniu).");
                return;
            }

            var newest = files[files.Count - 1];
            // Staleness guard: if newest segment is older than 8s, recorder was stalled/dead!
            if ((DateTime.UtcNow - newest.LastWriteTimeUtc).TotalSeconds > 8.0) {
                NotifyError("Bufor nie zawierał świeżych klatek (trwa restart bufora).");
                StopInternal(cleanBuffer: true);
                Start();
                return;
            }

            int segTime = 2;
            int duration = Math.Max(5, targetDuration);
            int neededCount = (int)Math.Ceiling(duration / (double)segTime) + 1;

            var chosen = files.Skip(Math.Max(0, files.Count - neededCount)).Select(f => f.FullName).ToList();
            if (chosen.Count == 0) {
                NotifyError("Niewystarczająca ilość danych w buforze.");
                return;
            }

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

            // Szybka jednoetapowa konkatenacja stream-copy bez zbędnego drugiego przejścia faststart
            string concatArgs = string.Format(
                "-y -nostats -loglevel error -f concat -safe 0 -i \"{0}\" -c copy -fflags +genpts -avoid_negative_ts make_zero \"{1}\"",
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

            if (File.Exists(outputClip) && new FileInfo(outputClip).Length > 0) {
                int actualDuration = chosen.Count * segTime;
                if (ClipSaved != null) {
                    ClipSaved(outputClip, actualDuration);
                }

                // Enforce storage limits asynchronously
                ThreadPool.QueueUserWorkItem(delegate { EnforceStorageLimits(); });
            } else {
                NotifyError("Nie udało się wygenerować pliku MP4 klipu.");
            }
        }

        public bool TrimClip(string inputPath, double startSec, double endSec, out string outputPath) {
            outputPath = null;
            try {
                if (!File.Exists(inputPath)) return false;
                if (endSec <= startSec) return false;

                string dir = Path.GetDirectoryName(inputPath);
                string filename = Path.GetFileNameWithoutExtension(inputPath);
                string ext = Path.GetExtension(inputPath);
                string timestamp = DateTime.Now.ToString("HHmmss");
                outputPath = Path.Combine(dir, string.Format("{0}_cut_{1}{2}", filename, timestamp, ext));

                string ffmpegPath = FindOrExtractFfmpeg();
                string durationArg = string.Format(System.Globalization.CultureInfo.InvariantCulture, "-ss {0:0.##} -to {1:0.##}", startSec, endSec);
                string args = string.Format("-y -nostats -loglevel error {0} -i \"{1}\" -c copy -avoid_negative_ts make_zero \"{2}\"", durationArg, inputPath, outputPath);

                var psi = new ProcessStartInfo {
                    FileName = ffmpegPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var proc = Process.Start(psi)) {
                    proc.WaitForExit(10000);
                }

                return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
            } catch {
                return false;
            }
        }

        public void EnforceStorageLimits() {
            try {
                string clipsDir = _settings.ClipsFolder;
                if (string.IsNullOrEmpty(clipsDir) || !Directory.Exists(clipsDir)) return;

                var dir = new DirectoryInfo(clipsDir);
                var mp4Files = dir.GetFiles("*.mp4");
                if (mp4Files.Length == 0) return;

                // 1. Delete clips older than MaxClipAgeDays
                if (_settings.MaxClipAgeDays > 0) {
                    DateTime cutoff = DateTime.UtcNow.AddDays(-_settings.MaxClipAgeDays);
                    foreach (var f in mp4Files) {
                        try {
                            if (f.LastWriteTimeUtc < cutoff) {
                                f.Delete();
                            }
                        } catch { }
                    }
                    mp4Files = dir.GetFiles("*.mp4");
                }

                // 2. Enforce folder size limit
                if (_settings.StorageLimitEnabled && _settings.MaxStorageLimitGB > 0) {
                    long maxBytes = (long)_settings.MaxStorageLimitGB * 1024L * 1024L * 1024L;
                    long targetBytes = (long)(maxBytes * 0.9);
                    long totalBytes = mp4Files.Sum(f => f.Length);

                    if (totalBytes > maxBytes) {
                        var sorted = mp4Files.OrderBy(f => f.LastWriteTimeUtc).ToList();
                        foreach (var f in sorted) {
                            try {
                                long len = f.Length;
                                f.Delete();
                                totalBytes -= len;
                                if (totalBytes <= targetBytes) break;
                            } catch { }
                        }
                    }
                }
            } catch { }
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
