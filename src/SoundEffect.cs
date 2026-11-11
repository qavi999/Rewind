using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ClipFlow {
    public static class SoundEffect {
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, byte[]> _cachedPresets = new Dictionary<string, byte[]>();
        private static SoundPlayer _activePlayer;
        private static string _currentPreset = "Default";
        private static string _currentCustomPath = "";
        private static bool _initialized = false;

        public static void Configure(AppSettings settings) {
            if (settings == null) return;
            lock (_lock) {
                _currentPreset = string.IsNullOrEmpty(settings.SoundPreset) ? "Default" : settings.SoundPreset;
                _currentCustomPath = settings.CustomSoundPath ?? "";
                ReloadActivePlayer();
            }
        }

        public static void Init() {
            if (_initialized) return;
            lock (_lock) {
                if (_initialized) return;
                try {
                    // Pre-generate procedural presets into memory cache (< 5ms)
                    _cachedPresets["Default"] = GenerateDefaultSound();
                    _cachedPresets["Shutter"] = GenerateShutterSound();
                    _cachedPresets["Ding"] = GenerateDingSound();
                    _cachedPresets["Retro"] = GenerateRetroSound();

                    // If embedded clip.wav exists, prefer it as "Default"
                    try {
                        var asm = Assembly.GetExecutingAssembly();
                        using (var resStream = asm.GetManifestResourceStream("clip.wav")) {
                            if (resStream != null) {
                                using (var ms = new MemoryStream()) {
                                    resStream.CopyTo(ms);
                                    _cachedPresets["Default"] = ms.ToArray();
                                }
                            }
                        }
                    } catch { }

                    ReloadActivePlayer();
                    _initialized = true;
                } catch {
                    _initialized = true;
                }
            }
        }

        private static void ReloadActivePlayer() {
            try {
                if (_currentPreset == "Custom" && !string.IsNullOrEmpty(_currentCustomPath) && File.Exists(_currentCustomPath)) {
                    string ext = Path.GetExtension(_currentCustomPath).ToLowerInvariant();
                    if (ext == ".wav") {
                        _activePlayer = new SoundPlayer(_currentCustomPath);
                        _activePlayer.Load();
                        return;
                    }
                    // For MP3/M4A, mciSendString will be used in Play()
                    _activePlayer = null;
                    return;
                }

                byte[] wavData;
                if (!_cachedPresets.TryGetValue(_currentPreset, out wavData)) {
                    if (!_cachedPresets.TryGetValue("Default", out wavData)) {
                        wavData = GenerateDefaultSound();
                    }
                }

                if (wavData != null) {
                    var ms = new MemoryStream(wavData);
                    _activePlayer = new SoundPlayer(ms);
                    _activePlayer.Load();
                }
            } catch {
                _activePlayer = null;
            }
        }

        public static void Play() {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    if (!_initialized) Init();

                    if (_currentPreset == "Custom" && !string.IsNullOrEmpty(_currentCustomPath) && File.Exists(_currentCustomPath)) {
                        string ext = Path.GetExtension(_currentCustomPath).ToLowerInvariant();
                        if (ext != ".wav") {
                            PlayFileViaMci(_currentCustomPath);
                            return;
                        }
                    }

                    if (_activePlayer != null) {
                        _activePlayer.Play();
                    } else {
                        SystemSounds.Asterisk.Play();
                    }
                } catch {
                    try { SystemSounds.Asterisk.Play(); } catch { }
                }
            });
        }

        public static void PlayPreview(string preset, string customPath) {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    if (!_initialized) Init();

                    if (preset == "Custom" && !string.IsNullOrEmpty(customPath) && File.Exists(customPath)) {
                        string ext = Path.GetExtension(customPath).ToLowerInvariant();
                        if (ext == ".wav") {
                            using (var p = new SoundPlayer(customPath)) {
                                p.Load();
                                p.Play();
                            }
                        } else {
                            PlayFileViaMci(customPath);
                        }
                        return;
                    }

                    byte[] data;
                    if (!_cachedPresets.TryGetValue(preset, out data)) {
                        if (!_cachedPresets.TryGetValue("Default", out data)) {
                            data = GenerateDefaultSound();
                        }
                    }

                    if (data != null) {
                        using (var ms = new MemoryStream(data))
                        using (var p = new SoundPlayer(ms)) {
                            p.Load();
                            p.Play();
                        }
                    } else {
                        SystemSounds.Asterisk.Play();
                    }
                } catch {
                    try { SystemSounds.Asterisk.Play(); } catch { }
                }
            });
        }

        private static void PlayFileViaMci(string filePath) {
            try {
                mciSendString("close rewind_preview", null, 0, IntPtr.Zero);
                string openCmd = string.Format("open \"{0}\" type mpegvideo alias rewind_preview", filePath);
                int res = mciSendString(openCmd, null, 0, IntPtr.Zero);
                if (res == 0) {
                    mciSendString("play rewind_preview from 0", null, 0, IntPtr.Zero);
                } else {
                    SystemSounds.Asterisk.Play();
                }
            } catch {
                SystemSounds.Asterisk.Play();
            }
        }

        // ---------------------------------------------------------------------
        // PROCEDURAL AUDIO SYNTHESIZERS (ZERO DEPENDENCY WAV GENERATORS)
        // ---------------------------------------------------------------------
        private static byte[] GenerateDefaultSound() {
            int rate = 44100;
            double dur = 0.24;
            int total = (int)(rate * dur);
            short[] s = new short[total];
            Random rng = new Random(42);

            for (int i = 0; i < total; i++) {
                double t = (double)i / rate;
                double v = 0;
                if (t < 0.03) {
                    double env = Math.Exp(-t * 220.0);
                    double noise = (rng.NextDouble() * 2.0 - 1.0) * 0.45;
                    double snap = Math.Sin(2.0 * Math.PI * 2400.0 * t) * 0.55;
                    v += (noise + snap) * env;
                }
                if (t >= 0.025) {
                    double dt = t - 0.025;
                    double env = Math.Exp(-dt * 14.0);
                    double t1 = Math.Sin(2.0 * Math.PI * 1174.66 * dt) * 0.45;
                    double t2 = Math.Sin(2.0 * Math.PI * 1760.0 * dt) * 0.35;
                    v += (t1 + t2) * env;
                }
                v = Math.Max(-1.0, Math.Min(1.0, v));
                s[i] = (short)(v * 32767);
            }
            return BuildWav(s, rate);
        }

        private static byte[] GenerateShutterSound() {
            int rate = 44100;
            double dur = 0.16;
            int total = (int)(rate * dur);
            short[] s = new short[total];
            Random rng = new Random(101);

            for (int i = 0; i < total; i++) {
                double t = (double)i / rate;
                double v = 0;
                if (t < 0.025) {
                    double env = Math.Exp(-t * 260.0);
                    v += ((rng.NextDouble() * 2.0 - 1.0) * 0.6 + Math.Sin(2.0 * Math.PI * 2200.0 * t) * 0.4) * env;
                }
                if (t >= 0.045 && t < 0.08) {
                    double dt = t - 0.045;
                    double env = Math.Exp(-dt * 220.0);
                    v += ((rng.NextDouble() * 2.0 - 1.0) * 0.5 + Math.Sin(2.0 * Math.PI * 1800.0 * dt) * 0.5) * env;
                }
                v = Math.Max(-1.0, Math.Min(1.0, v));
                s[i] = (short)(v * 32767);
            }
            return BuildWav(s, rate);
        }

        private static byte[] GenerateDingSound() {
            int rate = 44100;
            double dur = 0.35;
            int total = (int)(rate * dur);
            short[] s = new short[total];

            for (int i = 0; i < total; i++) {
                double t = (double)i / rate;
                double env = Math.Exp(-t * 8.5);
                double v = Math.Sin(2.0 * Math.PI * 1318.5 * t) * 0.60
                         + Math.Sin(2.0 * Math.PI * 2637.0 * t) * 0.28
                         + Math.Sin(2.0 * Math.PI * 3955.5 * t) * 0.12;
                v = Math.Max(-1.0, Math.Min(1.0, v * env));
                s[i] = (short)(v * 32767);
            }
            return BuildWav(s, rate);
        }

        private static byte[] GenerateRetroSound() {
            int rate = 44100;
            double dur = 0.22;
            int total = (int)(rate * dur);
            short[] s = new short[total];

            for (int i = 0; i < total; i++) {
                double t = (double)i / rate;
                double v = 0;
                if (t < 0.05) {
                    double tone = Math.Sin(2.0 * Math.PI * 987.77 * t);
                    v = (tone > 0 ? 0.45 : -0.45) * Math.Exp(-t * 10.0);
                } else {
                    double dt = t - 0.05;
                    double vib = Math.Sin(2.0 * Math.PI * 30.0 * dt) * 15.0;
                    double tone = Math.Sin(2.0 * Math.PI * (1318.5 + vib) * dt);
                    v = (tone > 0 ? 0.45 : -0.45) * Math.Exp(-dt * 9.0);
                }
                v = Math.Max(-1.0, Math.Min(1.0, v));
                s[i] = (short)(v * 32767);
            }
            return BuildWav(s, rate);
        }

        private static byte[] BuildWav(short[] samples, int sampleRate) {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms)) {
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + samples.Length * 2);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(samples.Length * 2);
                for (int i = 0; i < samples.Length; i++) {
                    bw.Write(samples[i]);
                }
                bw.Flush();
                return ms.ToArray();
            }
        }
    }
}
