using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace ClipFlow {
    public static class AutostartHelper {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "Rewind";
        private static readonly string[] OLD_APP_NAMES = new string[] { "Flashback", "ClipFlow" };

        public static bool IsAutostartEnabled() {
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false)) {
                    if (key != null) {
                        var val = key.GetValue(APP_NAME);
                        if (val != null) return true;
                        // Check legacy names
                        foreach (var old in OLD_APP_NAMES) {
                            var oldVal = key.GetValue(old);
                            if (oldVal != null) return true;
                        }
                    }
                }
            } catch { }
            return false;
        }

        public static bool SetAutostart(bool enable) {
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true)) {
                    if (key != null) {
                        // Remove old legacy entries if present
                        foreach (var old in OLD_APP_NAMES) {
                            try { key.DeleteValue(old, false); } catch { }
                        }

                        if (enable) {
                            string exePath = Process.GetCurrentProcess().MainModule.FileName;
                            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) {
                                exePath = Assembly.GetExecutingAssembly().Location;
                            }
                            key.SetValue(APP_NAME, string.Format("\"{0}\" --minimized", exePath));
                        } else {
                            key.DeleteValue(APP_NAME, false);
                        }
                        return true;
                    }
                }
            } catch { }
            return false;
        }
    }

    public class AppSettings {
        public int ClipDuration { get; set; }
        public int Fps { get; set; }
        public string Bitrate { get; set; }
        public string Hotkey { get; set; }
        public string ClipsFolder { get; set; }
        public bool BufferActive { get; set; }
        public bool PlaySound { get; set; }
        public bool StartMinimized { get; set; }
        public bool AutoStart { get; set; }
        public int SystemVolumePercent { get; set; }
        public int MicVolumePercent { get; set; }
        public string SelectedMicId { get; set; }
        public bool CaptureCursor { get; set; }
        public string Theme { get; set; }
        public string SoundPreset { get; set; }
        public string CustomSoundPath { get; set; }
        public bool ShowNotification { get; set; }
        public string PerformanceProfile { get; set; }
        public string Language { get; set; }
        public int SelectedDisplayIndex { get; set; }
        public string SelectedDisplayDevice { get; set; }
        public bool AdvancedMode { get; set; }
        public bool SeparateAudioTracks { get; set; }
        public bool SecondaryHotkeyEnabled { get; set; }
        public string SecondaryHotkey { get; set; }
        public int SecondaryClipDuration { get; set; }
        public bool StorageLimitEnabled { get; set; }
        public int MaxStorageLimitGB { get; set; }
        public int MaxClipAgeDays { get; set; }
        public string CaptureEngine { get; set; }

        public AppSettings() {
            CaptureEngine = "DirectX";
            Language = "pl";
            AdvancedMode = false;
            SeparateAudioTracks = false;
            SecondaryHotkeyEnabled = true;
            SecondaryHotkey = "F7";
            SecondaryClipDuration = 120;
            StorageLimitEnabled = false;
            MaxStorageLimitGB = 25;
            MaxClipAgeDays = 0;
            SelectedDisplayIndex = 0;
            SelectedDisplayDevice = "";
            ClipDuration = 30;
            Fps = 60;
            Bitrate = "12M";
            Hotkey = "F8";
            BufferActive = true;
            PlaySound = true;
            StartMinimized = false;
            AutoStart = false;
            CaptureCursor = false;
            Theme = "Dark";
            SoundPreset = "Default";
            CustomSoundPath = "";
            ShowNotification = true;
            PerformanceProfile = "Balanced";
            SystemVolumePercent = 100;
            MicVolumePercent = 100;
            SelectedMicId = "default";

            string videosClips = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Rewind");
            if (!Directory.Exists(videosClips)) {
                try { Directory.CreateDirectory(videosClips); } catch { }
            }
            ClipsFolder = videosClips;
        }

        public static string GetConfigPath() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string localConfig = Path.Combine(baseDir, "rewind_settings.json");

            try {
                string testFile = Path.Combine(baseDir, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(testFile, "1");
                File.Delete(testFile);
                return localConfig;
            } catch {
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rewind");
                if (!Directory.Exists(appDataDir)) {
                    try { Directory.CreateDirectory(appDataDir); } catch { }
                }
                return Path.Combine(appDataDir, "rewind_settings.json");
            }
        }

        public static AppSettings Load() {
            string path = GetConfigPath();
            try {
                // If new config doesn't exist, check legacy config files for smooth upgrade
                if (!File.Exists(path)) {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] oldFiles = new string[] {
                        Path.Combine(baseDir, "flashback_settings.json"),
                        Path.Combine(baseDir, "clipflow_settings.json"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flashback", "flashback_settings.json"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipFlow", "clipflow_settings.json")
                    };
                    foreach (var old in oldFiles) {
                        if (File.Exists(old)) {
                            try { File.Copy(old, path, true); break; } catch { }
                        }
                    }
                }

                if (File.Exists(path)) {
                    string json = File.ReadAllText(path);
                    var serializer = new JavaScriptSerializer();
                    var settings = serializer.Deserialize<AppSettings>(json);
                    if (settings != null) {
                        if (settings.SystemVolumePercent < 0 || settings.SystemVolumePercent > 1000) settings.SystemVolumePercent = 100;
                        if (settings.MicVolumePercent < 0 || settings.MicVolumePercent > 1000) settings.MicVolumePercent = 100;
                        if (string.IsNullOrEmpty(settings.SelectedMicId)) settings.SelectedMicId = "default";
                        if (settings.SelectedDisplayDevice == null) settings.SelectedDisplayDevice = "";
                        if (settings.SelectedDisplayIndex < 0) settings.SelectedDisplayIndex = 0;
                        if (string.IsNullOrEmpty(settings.Theme)) settings.Theme = "Dark";
                        if (string.IsNullOrEmpty(settings.SoundPreset)) settings.SoundPreset = "Default";
                        if (settings.CustomSoundPath == null) settings.CustomSoundPath = "";
                        if (string.IsNullOrEmpty(settings.PerformanceProfile)) settings.PerformanceProfile = "Balanced";
                        if (settings.ClipDuration < 5 || settings.ClipDuration > 600) settings.ClipDuration = 30;
                        if (settings.Fps < 15 || settings.Fps > 360) settings.Fps = 60;
                        if (string.IsNullOrEmpty(settings.Bitrate)) settings.Bitrate = "12M";
                        if (string.IsNullOrEmpty(settings.Hotkey)) settings.Hotkey = "F8";
                        if (string.IsNullOrEmpty(settings.SecondaryHotkey)) settings.SecondaryHotkey = "F7";
                        if (settings.SecondaryClipDuration < 5 || settings.SecondaryClipDuration > 600) settings.SecondaryClipDuration = 120;
                        if (settings.MaxStorageLimitGB <= 0) settings.MaxStorageLimitGB = 25;
                        if (string.IsNullOrEmpty(settings.Language)) settings.Language = "pl";

                        settings.AutoStart = AutostartHelper.IsAutostartEnabled();

                        if (string.IsNullOrEmpty(settings.ClipsFolder) || !Directory.Exists(settings.ClipsFolder)) {
                            try { Directory.CreateDirectory(settings.ClipsFolder); } catch { }
                        }
                        return settings;
                    }
                }
            } catch { }

            var def = new AppSettings();
            def.AutoStart = AutostartHelper.IsAutostartEnabled();
            def.Save();
            return def;
        }

        public void Save() {
            try {
                string path = GetConfigPath();
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(this);
                File.WriteAllText(path, json);
            } catch { }
        }
    }
}
