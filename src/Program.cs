using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace ClipFlow {
    public static class Program {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private const int SW_RESTORE = 9;
        private static Mutex _singleInstanceMutex;

        [STAThread]
        public static void Main(string[] args) {
            try {
                SetCurrentProcessExplicitAppUserModelID("Rewind.App");
            } catch { }

            bool isNewInstance;
            _singleInstanceMutex = new Mutex(true, "Global\\Rewind_SingleInstance_App", out isNewInstance);

            if (!isNewInstance) {
                // Bring existing instance to front
                Process current = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(current.ProcessName)) {
                    if (p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero) {
                        ShowWindow(p.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(p.MainWindowHandle);
                        break;
                    }
                }
                return;
            }

            try {
                var app = new System.Windows.Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var settings = AppSettings.Load();
                var engine = new BufferEngine(settings);
                var mainWindow = new MainWindow(settings, engine);

                bool startMinimized = settings.StartMinimized;
                if (args != null) {
                    for (int i = 0; i < args.Length; i++) {
                        string a = args[i].ToLowerInvariant();
                        if (a == "--minimized" || a == "-minimized" || a == "--autostart" || a == "-silent") {
                            startMinimized = true;
                            break;
                        }
                    }
                }

                mainWindow.InitializeSystemIntegration(startMinimized);

                app.Run();
            } catch (Exception ex) {
                System.Windows.MessageBox.Show(
                    "Wystąpił błąd podczas działania aplikacji Rewind:\n\n" + ex.Message,
                    "Rewind Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            } finally {
                if (_singleInstanceMutex != null) {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
            }
        }
    }
}
