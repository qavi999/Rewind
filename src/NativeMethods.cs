using System;
using System.Runtime.InteropServices;

namespace ClipFlow {
    public static class NativeMethods {
        public const int WM_HOTKEY = 0x0312;
        public const int HOTKEY_ID = 9001;
        public const int HOTKEY_SECONDARY_ID = 9002;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public const string REWIND_SHOW_MESSAGE_NAME = "REWIND_ACTIVATE_INSTANCE_MSG";
        public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public const uint VK_F7 = 0x76;
        public const uint VK_F8 = 0x77;
        public const uint VK_F9 = 0x78;
        public const uint VK_F10 = 0x79;
        public const uint VK_F11 = 0x7A;
        public const uint VK_F12 = 0x7B;
        public const uint VK_C = 0x43;

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        public enum DWM_WINDOW_CORNER_PREFERENCE {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;

        public static void ApplyModernDwmAttributes(IntPtr hWnd, bool isDark = true) {
            try {
                // Set Immersive Dark / Light Mode for title bar & system frame
                int darkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                // Force DWM Hardware Rounded Corners (Windows 11 & Windhawk compatible)
                int cornerRound = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerRound, sizeof(int));
            } catch {
                // Graceful fallback on older Windows versions
            }
        }

        public static bool ParseHotkey(string keyStr, out uint modifier, out uint vk) {
            modifier = MOD_NONE;
            vk = VK_F8;

            if (string.IsNullOrEmpty(keyStr)) return false;

            string clean = keyStr.Trim().ToUpperInvariant();
            if (clean == "F7") { vk = VK_F7; return true; }
            if (clean == "F8") { vk = VK_F8; return true; }
            if (clean == "F9") { vk = VK_F9; return true; }
            if (clean == "F10") { vk = VK_F10; return true; }
            if (clean == "F11") { vk = VK_F11; return true; }
            if (clean == "F12") { vk = VK_F12; return true; }
            if (clean == "ALT+C" || clean == "ALT + C") { modifier = MOD_ALT; vk = VK_C; return true; }

            string[] parts = clean.Split(new char[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            uint mod = MOD_NONE;
            uint keyVk = 0;

            for (int i = 0; i < parts.Length; i++) {
                string p = parts[i].Trim();
                if (p == "CTRL" || p == "CONTROL") {
                    mod |= MOD_CONTROL;
                } else if (p == "ALT") {
                    mod |= MOD_ALT;
                } else if (p == "SHIFT") {
                    mod |= MOD_SHIFT;
                } else if (p == "WIN" || p == "WINDOWS" || p == "SUPER") {
                    mod |= MOD_WIN;
                } else if (p.StartsWith("F") && p.Length >= 2 && p.Length <= 3) {
                    int fNum;
                    if (int.TryParse(p.Substring(1), out fNum) && fNum >= 1 && fNum <= 24) {
                        keyVk = (uint)(0x70 + (fNum - 1)); // 0x70 is VK_F1
                    }
                } else if (p == "INSERT" || p == "INS") {
                    keyVk = 0x2D;
                } else if (p == "DELETE" || p == "DEL") {
                    keyVk = 0x2E;
                } else if (p == "HOME") {
                    keyVk = 0x24;
                } else if (p == "END") {
                    keyVk = 0x23;
                } else if (p == "PAGEUP" || p == "PGUP") {
                    keyVk = 0x21;
                } else if (p == "PAGEDOWN" || p == "PGDN") {
                    keyVk = 0x22;
                } else if (p == "PAUSE") {
                    keyVk = 0x13;
                } else if (p == "PRINTSCREEN" || p == "PRTSC" || p == "SNAPSHOT") {
                    keyVk = 0x2C;
                } else if (p == "SPACE" || p == "SPACEBAR") {
                    keyVk = 0x20;
                } else if (p.Length == 1 && ((p[0] >= 'A' && p[0] <= 'Z') || (p[0] >= '0' && p[0] <= '9'))) {
                    keyVk = (uint)p[0];
                }
            }

            if (keyVk != 0) {
                modifier = mod;
                vk = keyVk;
                return true;
            }
            return false;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
        }

        public const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        public static int GetMonitorRefreshRate(string deviceName = null) {
            try {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(string.IsNullOrEmpty(deviceName) ? null : deviceName, ENUM_CURRENT_SETTINGS, ref dm)) {
                    if (dm.dmDisplayFrequency > 0) {
                        return dm.dmDisplayFrequency;
                    }
                }
            } catch { }
            return 60;
        }
    }

    // -------------------------------------------------------------------------
    // WINDOWS JOB OBJECT TRACKER (GUARANTEES NO ORPHANED CHILD PROCESSES)
    // -------------------------------------------------------------------------
    public static class ChildProcessTracker {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryLimit;
            public UIntPtr PeakJobMemoryLimit;
        }

        private static readonly IntPtr _jobHandle;

        static ChildProcessTracker() {
            try {
                _jobHandle = CreateJobObject(IntPtr.Zero, null);
                if (_jobHandle != IntPtr.Zero) {
                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                    int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                    IntPtr infoPtr = Marshal.AllocHGlobal(length);
                    try {
                        Marshal.StructureToPtr(info, infoPtr, false);
                        SetInformationJobObject(_jobHandle, JobObjectExtendedLimitInformation, infoPtr, (uint)length);
                    } finally {
                        Marshal.FreeHGlobal(infoPtr);
                    }
                }
            } catch { }
        }

        public static void AddProcess(System.Diagnostics.Process process) {
            if (_jobHandle != IntPtr.Zero && process != null) {
                try {
                    AssignProcessToJobObject(_jobHandle, process.Handle);
                } catch { }
            }
        }
    }
}
