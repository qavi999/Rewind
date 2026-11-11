using System;
using System.Runtime.InteropServices;

namespace ClipFlow {
    public static class NativeMethods {
        public const int WM_HOTKEY = 0x0312;
        public const int HOTKEY_ID = 9001;

        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;

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

        public static void ParseHotkey(string keyStr, out uint modifier, out uint vk) {
            modifier = MOD_NONE;
            vk = VK_F8;

            if (string.IsNullOrEmpty(keyStr)) return;

            string upper = keyStr.Trim().ToUpperInvariant();
            if (upper == "F7") { vk = VK_F7; }
            else if (upper == "F8") { vk = VK_F8; }
            else if (upper == "F9") { vk = VK_F9; }
            else if (upper == "F10") { vk = VK_F10; }
            else if (upper == "F11") { vk = VK_F11; }
            else if (upper == "F12") { vk = VK_F12; }
            else if (upper.Contains("ALT") && upper.Contains("C")) {
                modifier = MOD_ALT;
                vk = VK_C;
            }
        }
    }
}
