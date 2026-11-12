using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using Panel = System.Windows.Controls.Panel;
using Slider = System.Windows.Controls.Slider;

namespace ClipFlow {
    // -------------------------------------------------------------------------
    // RAYCAST TRAY CONTEXT MENU RENDERER (PURE JET BLACK)
    // -------------------------------------------------------------------------
    public class RaycastColorTable : ProfessionalColorTable {
        public override System.Drawing.Color MenuBorder { get { return System.Drawing.Color.FromArgb(42, 42, 42); } }
        public override System.Drawing.Color ToolStripDropDownBackground { get { return System.Drawing.Color.FromArgb(16, 16, 16); } }
        public override System.Drawing.Color ImageMarginGradientBegin { get { return System.Drawing.Color.FromArgb(16, 16, 16); } }
        public override System.Drawing.Color ImageMarginGradientMiddle { get { return System.Drawing.Color.FromArgb(16, 16, 16); } }
        public override System.Drawing.Color ImageMarginGradientEnd { get { return System.Drawing.Color.FromArgb(16, 16, 16); } }
        public override System.Drawing.Color MenuItemSelected { get { return System.Drawing.Color.FromArgb(32, 32, 32); } }
        public override System.Drawing.Color MenuItemBorder { get { return System.Drawing.Color.Transparent; } }
        public override System.Drawing.Color SeparatorDark { get { return System.Drawing.Color.FromArgb(32, 32, 32); } }
        public override System.Drawing.Color SeparatorLight { get { return System.Drawing.Color.Transparent; } }
    }

    public class RaycastMenuRenderer : ToolStripProfessionalRenderer {
        public RaycastMenuRenderer() : base(new RaycastColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            if (!e.Item.Enabled) return;
            Rectangle rc = new Rectangle(5, 1, e.Item.Width - 10, e.Item.Height - 2);
            if (e.Item.Selected) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(34, 34, 34)))
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 50, 50), 1))
                using (var path = GetRoundedRect(rc, 7)) {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            if (!e.Item.Enabled) {
                if (e.Item.Tag != null && e.Item.Tag.ToString() == "header") {
                    e.TextColor = System.Drawing.Color.FromArgb(139, 92, 246); // Electric Violet Accent
                } else if (e.Item.Tag != null && e.Item.Tag.ToString() == "status") {
                    e.TextColor = System.Drawing.Color.FromArgb(34, 197, 94); // Clean Emerald
                } else {
                    e.TextColor = System.Drawing.Color.FromArgb(130, 130, 130);
                }
            } else {
                e.TextColor = e.Item.Selected ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(235, 235, 235);
            }

            base.OnRenderItemText(e);

            var menuItem = e.Item as ToolStripMenuItem;
            if (menuItem != null && !string.IsNullOrEmpty(menuItem.ShortcutKeyDisplayString)) {
                string sc = menuItem.ShortcutKeyDisplayString;
                using (Font scFont = new Font(e.TextFont.FontFamily, 8.5f, System.Drawing.FontStyle.Regular)) {
                    System.Drawing.Size scSize = TextRenderer.MeasureText(sc, scFont);
                    int x = e.Item.Width - scSize.Width - 14;
                    int y = (e.Item.Height - scSize.Height) / 2;
                    System.Drawing.Color scColor = e.Item.Selected ? System.Drawing.Color.FromArgb(210, 210, 210) : System.Drawing.Color.FromArgb(120, 120, 120);
                    TextRenderer.DrawText(e.Graphics, sc, scFont, new System.Drawing.Point(x, y), scColor);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            int y = e.Item.Height / 2;
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(32, 32, 32), 1)) {
                e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            Rectangle rc = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(42, 42, 42))) {
                e.Graphics.DrawRectangle(pen, rc);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius) {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // -------------------------------------------------------------------------
    // RAYCAST THEME PALETTE (DARK & LIGHT MODE)
    // -------------------------------------------------------------------------
    public class ThemePalette {
        public static bool IsDarkActive = true;

        public bool IsDark { get; set; }
        public Brush WindowBg { get; set; }
        public Brush WindowBorder { get; set; }
        public Brush SidebarBg { get; set; }
        public Brush SidebarBorder { get; set; }
        public Brush ContentBg { get; set; }
        public Brush CardBg { get; set; }
        public Brush CardBorder { get; set; }
        public Brush RowDivider { get; set; }
        public Brush TextPrimary { get; set; }
        public Brush TextSecondary { get; set; }
        public Brush TextMuted { get; set; }
        public Brush InputBg { get; set; }
        public Brush InputBorder { get; set; }
        public Brush ButtonBg { get; set; }
        public Brush ButtonBorder { get; set; }
        public Brush ButtonHoverBg { get; set; }
        public Brush PillUnselectedBg { get; set; }
        public Brush PillUnselectedBorder { get; set; }
        public Brush PillUnselectedFg { get; set; }
        public Brush AccentCoral { get; set; }
        public Brush AccentCoralSoft { get; set; }
        public Brush StatusBadgeBg { get; set; }
        public Brush StatusBadgeBorder { get; set; }
        public Brush StatusBadgeFg { get; set; }

        public static ThemePalette Dark() {
            IsDarkActive = true;
            return new ThemePalette {
                IsDark = true,
                WindowBg = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                WindowBorder = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                SidebarBg = new SolidColorBrush(Color.FromRgb(15, 15, 15)),
                SidebarBorder = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                ContentBg = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                CardBg = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                CardBorder = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                RowDivider = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                TextPrimary = System.Windows.Media.Brushes.White,
                TextSecondary = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                TextMuted = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                InputBg = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                InputBorder = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                ButtonBg = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                ButtonBorder = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                ButtonHoverBg = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                PillUnselectedBg = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                PillUnselectedBorder = new SolidColorBrush(Color.FromRgb(36, 36, 36)),
                PillUnselectedFg = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                AccentCoral = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                AccentCoralSoft = new SolidColorBrush(Color.FromArgb(40, 139, 92, 246)),
                StatusBadgeBg = new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),
                StatusBadgeBorder = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                StatusBadgeFg = new SolidColorBrush(Color.FromRgb(187, 247, 208))
            };
        }

        public static ThemePalette Light() {
            IsDarkActive = false;
            return new ThemePalette {
                IsDark = false,
                WindowBg = new SolidColorBrush(Color.FromRgb(246, 246, 248)),
                WindowBorder = new SolidColorBrush(Color.FromRgb(218, 220, 226)),
                SidebarBg = new SolidColorBrush(Color.FromRgb(238, 239, 243)),
                SidebarBorder = new SolidColorBrush(Color.FromRgb(224, 226, 232)),
                ContentBg = new SolidColorBrush(Color.FromRgb(246, 246, 248)),
                CardBg = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CardBorder = new SolidColorBrush(Color.FromRgb(226, 228, 234)),
                RowDivider = new SolidColorBrush(Color.FromRgb(240, 241, 245)),
                TextPrimary = new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                TextSecondary = new SolidColorBrush(Color.FromRgb(100, 102, 112)),
                TextMuted = new SolidColorBrush(Color.FromRgb(140, 142, 150)),
                InputBg = new SolidColorBrush(Color.FromRgb(243, 244, 247)),
                InputBorder = new SolidColorBrush(Color.FromRgb(215, 218, 226)),
                ButtonBg = new SolidColorBrush(Color.FromRgb(242, 243, 246)),
                ButtonBorder = new SolidColorBrush(Color.FromRgb(216, 218, 226)),
                ButtonHoverBg = new SolidColorBrush(Color.FromRgb(230, 232, 238)),
                PillUnselectedBg = new SolidColorBrush(Color.FromRgb(238, 240, 244)),
                PillUnselectedBorder = new SolidColorBrush(Color.FromRgb(218, 220, 228)),
                PillUnselectedFg = new SolidColorBrush(Color.FromRgb(90, 92, 102)),
                AccentCoral = new SolidColorBrush(Color.FromRgb(124, 58, 237)),
                AccentCoralSoft = new SolidColorBrush(Color.FromArgb(28, 124, 58, 237)),
                StatusBadgeBg = new SolidColorBrush(Color.FromArgb(25, 22, 163, 74)),
                StatusBadgeBorder = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                StatusBadgeFg = new SolidColorBrush(Color.FromRgb(21, 128, 61))
            };
        }
    }

    // -------------------------------------------------------------------------
    // RAYCAST SLEEK PILL TOGGLE CONTROL
    // -------------------------------------------------------------------------
    public class RaycastToggle : ContentControl {
        private bool _isChecked;
        private Border _track;
        private Border _knob;

        public event Action<bool> CheckedChanged;

        public bool IsChecked {
            get { return _isChecked; }
            set {
                if (_isChecked != value) {
                    _isChecked = value;
                    UpdateVisual();
                    if (CheckedChanged != null) CheckedChanged(_isChecked);
                }
            }
        }

        public RaycastToggle(bool initialChecked = false) {
            _isChecked = initialChecked;
            Cursor = System.Windows.Input.Cursors.Hand;
            Width = 38;
            Height = 22;

            _track = new Border {
                Width = 38,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                BorderThickness = new Thickness(1)
            };

            _knob = new Border {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(_track);
            grid.Children.Add(_knob);
            Content = grid;

            UpdateVisual();

            MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) {
                IsChecked = !IsChecked;
                e.Handled = true;
            };
        }

        public void UpdateVisual() {
            if (_isChecked) {
                _track.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // Electric Violet
                _track.BorderBrush = new SolidColorBrush(Color.FromRgb(167, 139, 250));
                _knob.Background = System.Windows.Media.Brushes.White;
                _knob.HorizontalAlignment = HorizontalAlignment.Right;
                _knob.Margin = new Thickness(0, 0, 3, 0);
            } else {
                if (ThemePalette.IsDarkActive) {
                    _track.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                    _track.BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                    _knob.Background = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                } else {
                    _track.Background = new SolidColorBrush(Color.FromRgb(220, 222, 228));
                    _track.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 202, 210));
                    _knob.Background = System.Windows.Media.Brushes.White;
                }
                _knob.HorizontalAlignment = HorizontalAlignment.Left;
                _knob.Margin = new Thickness(3, 0, 0, 0);
            }
        }
    }

    // -------------------------------------------------------------------------
    // DISCREET GAMING OVERLAY HUD TOAST NOTIFICATION WINDOW
    // -------------------------------------------------------------------------
    public class HudNotificationWindow : Window {
        private static HudNotificationWindow _currentInstance;
        private System.Windows.Threading.DispatcherTimer _timer;

        public static void ShowToast(string fileName, int duration, string clipPath, ThemePalette theme, UiStrings strings = null) {
            try {
                if (_currentInstance != null) {
                    try { _currentInstance.Close(); } catch { }
                    _currentInstance = null;
                }

                var hud = new HudNotificationWindow(fileName, duration, clipPath, theme, strings);
                _currentInstance = hud;
                hud.Show();
            } catch { }
        }

        public HudNotificationWindow(string fileName, int duration, string clipPath, ThemePalette theme, UiStrings strings = null) {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Width = 350;
            Height = 74;
            Background = System.Windows.Media.Brushes.Transparent;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif");

            Left = SystemParameters.WorkArea.Right - 370;
            Top = SystemParameters.WorkArea.Bottom - 94;

            bool isDark = theme == null || theme.IsDark;

            var outer = new Border {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(isDark ? Color.FromArgb(235, 18, 18, 18) : Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(isDark ? Color.FromRgb(40, 40, 40) : Color.FromRgb(220, 222, 228)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = isDark ? 0.45 : 0.15
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); // Icon badge
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Text
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action button

            var iconBox = new Border {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(9),
                Background = new System.Windows.Media.LinearGradientBrush(
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(99, 102, 241),
                    45.0
                ),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock {
                    Text = "✦",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(iconBox, 0);
            grid.Children.Add(iconBox);

            var textStack = new StackPanel {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            string titleFormat = strings != null ? strings.HudTitleFormat : "Klip powtórki zapisany! ({0}s)";
            string openText = strings != null ? strings.HudOpenBtn : "Otwórz";

            var titleTb = new TextBlock {
                Text = string.Format(titleFormat, duration),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = isDark ? System.Windows.Media.Brushes.White : new SolidColorBrush(Color.FromRgb(20, 20, 24))
            };
            textStack.Children.Add(titleTb);

            var fileTb = new TextBlock {
                Text = fileName,
                FontSize = 10.5,
                Foreground = isDark ? new SolidColorBrush(Color.FromRgb(140, 140, 140)) : new SolidColorBrush(Color.FromRgb(100, 102, 110)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            textStack.Children.Add(fileTb);

            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var openBtn = new Button {
                Content = openText,
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(10, 5, 10, 5),
                Background = isDark ? new SolidColorBrush(Color.FromRgb(32, 32, 32)) : new SolidColorBrush(Color.FromRgb(240, 242, 246)),
                BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(50, 50, 50)) : new SolidColorBrush(Color.FromRgb(215, 218, 226)),
                BorderThickness = new Thickness(1),
                Foreground = isDark ? System.Windows.Media.Brushes.White : new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                VerticalAlignment = VerticalAlignment.Center
            };
            MainWindow.StyleModernButton(openBtn, 8);
            openBtn.Click += delegate {
                try {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", clipPath));
                } catch { }
                Close();
            };
            Grid.SetColumn(openBtn, 2);
            grid.Children.Add(openBtn);

            outer.Child = grid;
            outer.MouseLeftButtonDown += delegate {
                try {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", clipPath));
                } catch { }
                Close();
            };

            Content = outer;

            _timer = new System.Windows.Threading.DispatcherTimer {
                Interval = TimeSpan.FromSeconds(3.5)
            };
            _timer.Tick += delegate {
                _timer.Stop();
                Close();
            };
            _timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST);
        }
    }

    // -------------------------------------------------------------------------
    // MAIN REWIND WINDOW (RAYCAST PREFERENCES UI)
    // -------------------------------------------------------------------------
    public class MainWindow : Window {
        private readonly AppSettings _settings;
        private readonly BufferEngine _engine;
        private NotifyIcon _notifyIcon;
        private ToolStripMenuItem _trayBufferToggleItem;
        private ToolStripMenuItem _trayStatusItem;
        private bool _isExiting;
        private IntPtr _hwnd;
        private HwndSource _hwndSource;

        // Active Navigation
        private int _activeCategoryIndex = 0;
        private List<Button> _navButtons = new List<Button>();
        private TextBlock _breadcrumbText;

        // View Panels
        private Grid _contentArea;
        private ScrollViewer _viewClips;
        private ScrollViewer _viewVideo;
        private ScrollViewer _viewAudio;
        private ScrollViewer _viewHotkeys;
        private ScrollViewer _viewSystem;
        private ScrollViewer _viewAbout;

        // Sidebar Footer Info
        private System.Windows.Shapes.Ellipse _sidebarStatusDot;
        private TextBlock _sidebarStatusText;
        private TextBlock _sidebarInfoSubtext;

        // Clips View Controls
        private TextBlock _heroTitle;
        private TextBlock _heroHotkeyBadge;
        private Button _heroClipBtn;
        private Border _toastBanner;
        private TextBlock _toastText;
        private Button _toastOpenBtn;
        private string _lastSavedClipPath;
        private StackPanel _clipsListPanel;
        private TextBlock _clipsCountBadge;
        private TextBlock _emptyNotice;

        // Video Settings Controls
        private TextBlock _screenDropdownText;
        private List<Button> _durationButtons = new List<Button>();
        private List<Button> _fpsButtons = new List<Button>();
        private List<Button> _bitrateButtons = new List<Button>();
        private TextBlock _folderPathText;

        // Audio Settings Controls
        private TextBlock _micDropdownText;
        private TextBlock _sysVolLabel;
        private TextBlock _micVolLabel;
        private Slider _sysVolSlider;
        private Slider _micVolSlider;
        private List<Button> _soundPresetButtons = new List<Button>();
        private TextBlock _customSoundPathText;

        // Hotkeys, System & Theme Controls
        private List<Button> _hotkeyButtons = new List<Button>();
        private List<Button> _themeButtons = new List<Button>();
        private Button _toggleBufferBtn;
        private ThemePalette _theme;
        private UiStrings _L;

        public MainWindow(AppSettings settings, BufferEngine engine) {
            _settings = settings;
            _engine = engine;
            _L = AppLocalization.Get(_settings.Language);
            _theme = (_settings.Theme == "Light") ? ThemePalette.Light() : ThemePalette.Dark();
            SoundEffect.Configure(_settings);

            ApplyModernScrollbarStyle(_theme.IsDark);
            InitWindow();
            BuildRaycastPreferencesLayout();
            InitTrayIcon();
            HookEngineEvents();

            Closing += OnWindowClosing;
            KeyDown += OnWindowKeyDown;
        }

        public void SwitchLanguage(string langCode) {
            _settings.Language = langCode;
            _settings.Save();
            _L = AppLocalization.Get(langCode);

            BuildRaycastPreferencesLayout();
            SwitchCategory(_activeCategoryIndex);
            InitTrayIcon();
            UpdateHeroTitle();
            if (_sidebarStatusText != null) {
                _sidebarStatusText.Text = _engine.IsRecording ? _L.SidebarStatusActive : _L.SidebarStatusPaused;
            }
            RefreshClipsList();
        }

        public void ApplyTheme(bool isDark) {
            _settings.Theme = isDark ? "Dark" : "Light";
            _settings.Save();
            _theme = isDark ? ThemePalette.Dark() : ThemePalette.Light();
            Background = _theme.WindowBg;
            Foreground = _theme.TextPrimary;
            if (_hwnd != IntPtr.Zero) {
                NativeMethods.ApplyModernDwmAttributes(_hwnd, isDark);
            }
            ApplyModernScrollbarStyle(isDark);
            BuildRaycastPreferencesLayout();
            SwitchCategory(_activeCategoryIndex);
        }

        private void ApplyModernScrollbarStyle(bool isDark) {
            try {
                string thumbColor = isDark ? "#3E3E44" : "#C8CAD2";
                string hoverColor = isDark ? "#666672" : "#9EA0AC";
                string dragColor = isDark ? "#8B5CF6" : "#7C3AED";

                string xaml = string.Format(@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <ControlTemplate x:Key=""VerticalScrollBarTemplate"" TargetType=""{{x:Type ScrollBar}}"">
        <Grid x:Name=""Bg"" Background=""Transparent"" SnapsToDevicePixels=""true"">
            <Track x:Name=""PART_Track"" IsDirectionReversed=""true"" HorizontalAlignment=""Center"">
                <Track.DecreaseRepeatButton>
                    <RepeatButton Command=""{{x:Static ScrollBar.PageUpCommand}}"" Focusable=""false"" Opacity=""0"">
                        <RepeatButton.Template>
                            <ControlTemplate TargetType=""{{x:Type RepeatButton}}"">
                                <Rectangle Fill=""Transparent""/>
                            </ControlTemplate>
                        </RepeatButton.Template>
                    </RepeatButton>
                </Track.DecreaseRepeatButton>
                <Track.IncreaseRepeatButton>
                    <RepeatButton Command=""{{x:Static ScrollBar.PageDownCommand}}"" Focusable=""false"" Opacity=""0"">
                        <RepeatButton.Template>
                            <ControlTemplate TargetType=""{{x:Type RepeatButton}}"">
                                <Rectangle Fill=""Transparent""/>
                            </ControlTemplate>
                        </RepeatButton.Template>
                    </RepeatButton>
                </Track.IncreaseRepeatButton>
                <Track.Thumb>
                    <Thumb>
                        <Thumb.Template>
                            <ControlTemplate TargetType=""{{x:Type Thumb}}"">
                                <Border x:Name=""thumbBorder""
                                        Width=""6""
                                        CornerRadius=""3""
                                        Margin=""1,2,1,2""
                                        Opacity=""0.65""
                                        Background=""{0}""/>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""true"">
                                        <Setter TargetName=""thumbBorder"" Property=""Opacity"" Value=""1.0""/>
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{1}""/>
                                    </Trigger>
                                    <Trigger Property=""IsDragging"" Value=""true"">
                                        <Setter TargetName=""thumbBorder"" Property=""Opacity"" Value=""1.0""/>
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{2}""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Thumb.Template>
                    </Thumb>
                </Track.Thumb>
            </Track>
        </Grid>
    </ControlTemplate>

    <ControlTemplate x:Key=""HorizontalScrollBarTemplate"" TargetType=""{{x:Type ScrollBar}}"">
        <Grid x:Name=""Bg"" Background=""Transparent"" SnapsToDevicePixels=""true"">
            <Track x:Name=""PART_Track"" IsDirectionReversed=""false"" VerticalAlignment=""Center"">
                <Track.DecreaseRepeatButton>
                    <RepeatButton Command=""{{x:Static ScrollBar.PageLeftCommand}}"" Focusable=""false"" Opacity=""0"">
                        <RepeatButton.Template>
                            <ControlTemplate TargetType=""{{x:Type RepeatButton}}"">
                                <Rectangle Fill=""Transparent""/>
                            </ControlTemplate>
                        </RepeatButton.Template>
                    </RepeatButton>
                </Track.DecreaseRepeatButton>
                <Track.IncreaseRepeatButton>
                    <RepeatButton Command=""{{x:Static ScrollBar.PageRightCommand}}"" Focusable=""false"" Opacity=""0"">
                        <RepeatButton.Template>
                            <ControlTemplate TargetType=""{{x:Type RepeatButton}}"">
                                <Rectangle Fill=""Transparent""/>
                            </ControlTemplate>
                        </RepeatButton.Template>
                    </RepeatButton>
                </Track.IncreaseRepeatButton>
                <Track.Thumb>
                    <Thumb>
                        <Thumb.Template>
                            <ControlTemplate TargetType=""{{x:Type Thumb}}"">
                                <Border x:Name=""thumbBorder""
                                        Height=""6""
                                        CornerRadius=""3""
                                        Margin=""2,1,2,1""
                                        Opacity=""0.65""
                                        Background=""{0}""/>
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""true"">
                                        <Setter TargetName=""thumbBorder"" Property=""Opacity"" Value=""1.0""/>
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{1}""/>
                                    </Trigger>
                                    <Trigger Property=""IsDragging"" Value=""true"">
                                        <Setter TargetName=""thumbBorder"" Property=""Opacity"" Value=""1.0""/>
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{2}""/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Thumb.Template>
                    </Thumb>
                </Track.Thumb>
            </Track>
        </Grid>
    </ControlTemplate>

    <Style TargetType=""{{x:Type ScrollBar}}"">
        <Setter Property=""Stylus.IsPressAndHoldEnabled"" Value=""false""/>
        <Setter Property=""Stylus.IsFlicksEnabled"" Value=""false""/>
        <Setter Property=""Background"" Value=""Transparent""/>
        <Style.Triggers>
            <Trigger Property=""Orientation"" Value=""Vertical"">
                <Setter Property=""Width"" Value=""8""/>
                <Setter Property=""MinWidth"" Value=""8""/>
                <Setter Property=""Height"" Value=""Auto""/>
                <Setter Property=""Template"" Value=""{{StaticResource VerticalScrollBarTemplate}}""/>
            </Trigger>
            <Trigger Property=""Orientation"" Value=""Horizontal"">
                <Setter Property=""Width"" Value=""Auto""/>
                <Setter Property=""Height"" Value=""8""/>
                <Setter Property=""MinHeight"" Value=""8""/>
                <Setter Property=""Template"" Value=""{{StaticResource HorizontalScrollBarTemplate}}""/>
            </Trigger>
        </Style.Triggers>
    </Style>

</ResourceDictionary>", thumbColor, hoverColor, dragColor);

                var rd = (ResourceDictionary)XamlReader.Parse(xaml);
                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(rd);
            } catch { }
        }

        public static void StyleModernButton(Button btn, double cornerRadius) {
            var template = new ControlTemplate(typeof(Button));
            var borderFef = new FrameworkElementFactory(typeof(Border));
            borderFef.Name = "border";
            borderFef.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            borderFef.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFef.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFef.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFef.SetBinding(Border.PaddingProperty, new Binding("Padding") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

            var cpFef = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFef.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new Binding("HorizontalContentAlignment") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            cpFef.SetBinding(ContentPresenter.VerticalAlignmentProperty, new Binding("VerticalContentAlignment") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
            borderFef.AppendChild(cpFef);
            template.VisualTree = borderFef;
            btn.Template = template;
        }

        private void InitWindow() {
            Title = "Rewind";
            Width = 880;
            Height = 620;
            MinWidth = 780;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = _theme.WindowBg;
            Foreground = _theme.TextPrimary;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif");
            UseLayoutRounding = true;

            var chrome = new WindowChrome {
                CaptionHeight = 46,
                CornerRadius = new CornerRadius(14),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            var iconImg = LoadAppIconImage();
            if (iconImg != null) {
                Icon = iconImg;
            }
        }

        private static System.Drawing.Icon LoadAppIcon() {
            try {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("app.ico")) {
                    if (stream != null) {
                        return new System.Drawing.Icon(stream, 32, 32);
                    }
                }
            } catch { }

            try {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                var ico = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (ico != null) return ico;
            } catch { }

            return SystemIcons.Application;
        }

        private static ImageSource LoadAppIconImage() {
            try {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("app.ico")) {
                    if (stream != null) {
                        var ms = new MemoryStream();
                        byte[] buf = new byte[8192];
                        int r;
                        while ((r = stream.Read(buf, 0, buf.Length)) > 0) {
                            ms.Write(buf, 0, r);
                        }
                        ms.Position = 0;
                        var decoder = new IconBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        BitmapFrame bestFrame = null;
                        foreach (var frame in decoder.Frames) {
                            if (bestFrame == null || frame.PixelWidth > bestFrame.PixelWidth) {
                                bestFrame = frame;
                            }
                        }
                        if (bestFrame != null) return bestFrame;
                        if (decoder.Frames.Count > 0) {
                            return decoder.Frames[0];
                        }
                    }
                }
            } catch { }
            return null;
        }

        // ---------------------------------------------------------------------
        // RAYCAST TWO-COLUMN PREFERENCES LAYOUT
        // ---------------------------------------------------------------------
        private void BuildRaycastPreferencesLayout() {
            var rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(226) }); // Sidebar
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) }); // Divider
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content Area

            var outerBorder = new Border {
                BorderBrush = _theme.WindowBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Background = _theme.WindowBg,
                Child = rootGrid
            };
            Content = outerBorder;

            // 1. LEFT SIDEBAR
            var sidebar = CreateSidebar();
            Grid.SetColumn(sidebar, 0);
            rootGrid.Children.Add(sidebar);

            // 2. VERTICAL DIVIDER
            var divider = new Border {
                Background = _theme.SidebarBorder
            };
            Grid.SetColumn(divider, 1);
            rootGrid.Children.Add(divider);

            // 3. RIGHT CONTENT AREA
            var rightPanel = CreateRightPanel();
            Grid.SetColumn(rightPanel, 2);
            rootGrid.Children.Add(rightPanel);
        }

        private FrameworkElement CreateSidebar() {
            var grid = new Grid {
                Background = _theme.SidebarBg
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) }); // Brand Area
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Nav list
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) }); // Status card footer

            // Brand Header
            var brandStack = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 16, 0)
            };

            var logoBox = new Border {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(8),
                Background = new System.Windows.Media.LinearGradientBrush(
                    Color.FromRgb(139, 92, 246), // Electric Violet
                    Color.FromRgb(99, 102, 241),
                    45.0
                ),
                Margin = new Thickness(0, 0, 10, 0),
                Child = new TextBlock {
                    Text = "✦",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            brandStack.Children.Add(logoBox);

            var titleText = new TextBlock {
                Text = "Rewind",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = _theme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            brandStack.Children.Add(titleText);

            var verBadge = new Border {
                Background = _theme.PillUnselectedBg,
                BorderBrush = _theme.PillUnselectedBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock {
                    Text = "v0.1",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = _theme.TextSecondary
                }
            };
            brandStack.Children.Add(verBadge);

            Grid.SetRow(brandStack, 0);
            grid.Children.Add(brandStack);

            // Nav List
            var navScroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(10, 4, 10, 4)
            };

            var navStack = new StackPanel { Orientation = Orientation.Vertical };
            string[] navLabels = new string[] {
                _L.NavClips,
                _L.NavVideo,
                _L.NavAudio,
                _L.NavHotkeys,
                _L.NavSystem,
                _L.NavAbout
            };

            _navButtons.Clear();
            for (int i = 0; i < navLabels.Length; i++) {
                int catIndex = i;
                var btn = CreateSidebarNavButton(navLabels[i], i == _activeCategoryIndex);
                btn.Click += delegate { SwitchCategory(catIndex); };
                _navButtons.Add(btn);
                navStack.Children.Add(btn);
            }

            navScroll.Content = navStack;
            Grid.SetRow(navScroll, 1);
            grid.Children.Add(navScroll);

            // Bottom Status Card
            var footerBorder = new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(12, 6, 12, 12),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var footerStack = new StackPanel { Orientation = Orientation.Vertical };

            var statusRow = new StackPanel { Orientation = Orientation.Horizontal };
            _sidebarStatusDot = new System.Windows.Shapes.Ellipse {
                Width = 7,
                Height = 7,
                Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _sidebarStatusText = new TextBlock {
                Text = _settings.BufferActive ? _L.SidebarStatusActive : _L.SidebarStatusPaused,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.StatusBadgeFg,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusRow.Children.Add(_sidebarStatusDot);
            statusRow.Children.Add(_sidebarStatusText);
            footerStack.Children.Add(statusRow);

            _sidebarInfoSubtext = new TextBlock {
                Text = string.Format(_L.SidebarInfoFormat, _settings.Hotkey, _settings.ClipDuration),
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                Margin = new Thickness(13, 3, 0, 0)
            };
            footerStack.Children.Add(_sidebarInfoSubtext);

            footerBorder.Child = footerStack;
            Grid.SetRow(footerBorder, 2);
            grid.Children.Add(footerBorder);

            return grid;
        }

        private Button CreateSidebarNavButton(string label, bool isActive) {
            var btn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Height = 36,
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(14, 0, 12, 0)
            };
            StyleModernButton(btn, 10);

            var tb = new TextBlock {
                Text = label,
                FontSize = 12.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Content = tb;

            ApplySidebarNavStyle(btn, isActive);

            btn.MouseEnter += delegate {
                if (_navButtons.IndexOf(btn) != _activeCategoryIndex) {
                    btn.Background = _theme.ButtonHoverBg;
                    btn.BorderBrush = _theme.ButtonBorder;
                    tb.Foreground = _theme.TextPrimary;
                }
            };
            btn.MouseLeave += delegate {
                if (_navButtons.IndexOf(btn) != _activeCategoryIndex) {
                    btn.Background = System.Windows.Media.Brushes.Transparent;
                    btn.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    tb.Foreground = _theme.TextSecondary;
                }
            };

            return btn;
        }

        private void ApplySidebarNavStyle(Button btn, bool isActive) {
            var tb = btn.Content as TextBlock;
            if (isActive) {
                btn.Background = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(34, 34, 34)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
                btn.BorderBrush = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(56, 56, 56)) : new SolidColorBrush(Color.FromRgb(218, 220, 228));
                btn.BorderThickness = new Thickness(1);
                if (tb != null) {
                    tb.Foreground = _theme.IsDark ? System.Windows.Media.Brushes.White : new SolidColorBrush(Color.FromRgb(20, 20, 24));
                    tb.FontWeight = FontWeights.SemiBold;
                }
            } else {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.BorderBrush = System.Windows.Media.Brushes.Transparent;
                btn.BorderThickness = new Thickness(1);
                if (tb != null) {
                    tb.Foreground = _theme.TextSecondary;
                    tb.FontWeight = FontWeights.Normal;
                }
            }
        }

        private static string CleanNavTitle(string nav) {
            if (string.IsNullOrEmpty(nav)) return "";
            int idx = nav.IndexOf("  ");
            return (idx >= 0) ? nav.Substring(idx + 2).Trim() : nav.Trim();
        }

        private void SwitchCategory(int index) {
            _activeCategoryIndex = index;
            for (int i = 0; i < _navButtons.Count; i++) {
                ApplySidebarNavStyle(_navButtons[i], i == index);
            }

            string[] categoryNames = new string[] {
                _L.NavClips,
                _L.NavVideo,
                _L.NavAudio,
                _L.NavHotkeys,
                _L.NavSystem,
                _L.NavAbout
            };

            if (index >= 0 && index < categoryNames.Length) {
                _breadcrumbText.Text = "Rewind › " + CleanNavTitle(categoryNames[index]);
            }

            _viewClips.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            _viewVideo.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            _viewAudio.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;
            _viewHotkeys.Visibility = (index == 3) ? Visibility.Visible : Visibility.Collapsed;
            _viewSystem.Visibility = (index == 4) ? Visibility.Visible : Visibility.Collapsed;
            _viewAbout.Visibility = (index == 5) ? Visibility.Visible : Visibility.Collapsed;

            if (index == 0) {
                RefreshClipsList();
            }
        }

        private FrameworkElement CreateRightPanel() {
            var grid = new Grid {
                Background = _theme.ContentBg
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); // Caption / Header Bar
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Views
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) }); // Subtle Footer

            // 1. Caption Bar
            var captionGrid = new Grid();
            captionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            captionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _breadcrumbText = new TextBlock {
                Text = "Rewind › " + CleanNavTitle(_L.NavClips),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 0, 0, 0)
            };
            Grid.SetColumn(_breadcrumbText, 0);
            captionGrid.Children.Add(_breadcrumbText);

            var captionButtons = new StackPanel { Orientation = Orientation.Horizontal };

            var minBtn = CreateWindowsCaptionButton("—", _L.TooltipMinimize, false);
            minBtn.Click += delegate {
                WindowState = WindowState.Minimized;
                Hide();
            };
            captionButtons.Children.Add(minBtn);

            var closeBtn = CreateWindowsCaptionButton("✕", _L.TooltipClose, true);
            closeBtn.Click += delegate {
                Hide();
            };
            captionButtons.Children.Add(closeBtn);

            Grid.SetColumn(captionButtons, 1);
            captionGrid.Children.Add(captionButtons);

            Grid.SetRow(captionGrid, 0);
            grid.Children.Add(captionGrid);

            // 2. View Container
            _contentArea = new Grid();

            _viewClips = CreateClipsView();
            _viewVideo = CreateVideoSettingsView();
            _viewAudio = CreateAudioSettingsView();
            _viewHotkeys = CreateHotkeysSettingsView();
            _viewSystem = CreateSystemSettingsView();
            _viewAbout = CreateAboutView();

            _viewVideo.Visibility = Visibility.Collapsed;
            _viewAudio.Visibility = Visibility.Collapsed;
            _viewHotkeys.Visibility = Visibility.Collapsed;
            _viewSystem.Visibility = Visibility.Collapsed;
            _viewAbout.Visibility = Visibility.Collapsed;

            _contentArea.Children.Add(_viewClips);
            _contentArea.Children.Add(_viewVideo);
            _contentArea.Children.Add(_viewAudio);
            _contentArea.Children.Add(_viewHotkeys);
            _contentArea.Children.Add(_viewSystem);
            _contentArea.Children.Add(_viewAbout);

            Grid.SetRow(_contentArea, 1);
            grid.Children.Add(_contentArea);

            // 3. Subtle Footer
            var footerBorder = new Border {
                Background = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(13, 13, 13)) : new SolidColorBrush(Color.FromRgb(240, 241, 245)),
                BorderBrush = _theme.RowDivider,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 0, 24, 0)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var engineNotice = new TextBlock {
                Text = "⚡ NVIDIA NVENC Hardware Encoded • WASAPI Studio Audio",
                FontSize = 10.5,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(engineNotice, 0);
            footerGrid.Children.Add(engineNotice);

            var hintText = new TextBlock {
                Text = _L.FooterEscHint,
                FontSize = 10.5,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(hintText, 1);
            footerGrid.Children.Add(hintText);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 2);
            grid.Children.Add(footerBorder);

            return grid;
        }

        private Button CreateWindowsCaptionButton(string glyph, string tooltip, bool isClose) {
            var btn = new Button {
                Width = 46,
                Height = 46,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip
            };
            StyleModernButton(btn, 0);
            WindowChrome.SetIsHitTestVisibleInChrome(btn, true);

            var text = new TextBlock {
                Text = glyph,
                FontSize = isClose ? 11 : 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Content = text;

            btn.MouseEnter += delegate {
                if (isClose) {
                    btn.Background = new SolidColorBrush(Color.FromRgb(232, 17, 35));
                    text.Foreground = System.Windows.Media.Brushes.White;
                } else {
                    btn.Background = _theme.ButtonHoverBg;
                    text.Foreground = _theme.TextPrimary;
                }
            };

            btn.MouseLeave += delegate {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                text.Foreground = _theme.TextSecondary;
            };

            return btn;
        }

        // ---------------------------------------------------------------------
        // VIEW 1: MOJE POWTÓRKI (CLIPS & GALLERY)
        // ---------------------------------------------------------------------
        private ScrollViewer CreateClipsView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            stack.Children.Add(CreateHeroActionCard());
            stack.Children.Add(CreateToastBanner());
            stack.Children.Add(CreateClipsSection());

            scroll.Content = stack;
            return scroll;
        }

        private FrameworkElement CreateHeroActionCard() {
            var card = new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 18, 20, 18),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var hotkeyPill = new Border {
                Background = _theme.AccentCoralSoft,
                BorderBrush = _theme.AccentCoral,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 2, 7, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _heroHotkeyBadge = new TextBlock {
                Text = _L.HeroHotkeyPrefix + _settings.Hotkey,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(216, 180, 254)) : _theme.AccentCoral
            };
            hotkeyPill.Child = _heroHotkeyBadge;
            leftStack.Children.Add(hotkeyPill);

            _heroTitle = new TextBlock {
                Text = string.Format(_L.HeroTitleFormat, _settings.ClipDuration),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 4)
            };
            leftStack.Children.Add(_heroTitle);

            var descText = new TextBlock {
                Text = _L.HeroSubtitle,
                FontSize = 12,
                Foreground = _theme.TextSecondary
            };
            leftStack.Children.Add(descText);

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            _heroClipBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new System.Windows.Media.LinearGradientBrush(
                    Color.FromRgb(139, 92, 246), // Electric Violet Accent
                    Color.FromRgb(109, 40, 217),
                    45.0
                ),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                Padding = new Thickness(22, 10, 22, 10),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            StyleModernButton(_heroClipBtn, 14); // Rounded button pill

            var btnBorder = new Border {
                Child = new TextBlock { Text = _L.HeroButton }
            };
            _heroClipBtn.Content = btnBorder;
            _heroClipBtn.Click += delegate {
                _engine.SaveClip();
            };

            Grid.SetColumn(_heroClipBtn, 1);
            grid.Children.Add(_heroClipBtn);

            card.Child = grid;
            return card;
        }

        private FrameworkElement CreateToastBanner() {
            _toastBanner = new Border {
                Background = new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 9, 14, 9),
                Margin = new Thickness(0, 0, 0, 12),
                Visibility = Visibility.Collapsed
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _toastText = new TextBlock {
                Text = _L.ToastClipSaved,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = _theme.StatusBadgeFg,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_toastText, 0);
            grid.Children.Add(_toastText);

            _toastOpenBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Padding = new Thickness(12, 5, 12, 5),
                BorderThickness = new Thickness(0),
                Content = _L.ToastPlayNow
            };
            StyleModernButton(_toastOpenBtn, 10);
            _toastOpenBtn.Click += delegate {
                if (!string.IsNullOrEmpty(_lastSavedClipPath)) {
                    try { Process.Start(new ProcessStartInfo(_lastSavedClipPath) { UseShellExecute = true }); } catch { }
                }
            };
            Grid.SetColumn(_toastOpenBtn, 1);
            grid.Children.Add(_toastOpenBtn);

            _toastBanner.Child = grid;
            return _toastBanner;
        }

        private FrameworkElement CreateClipsSection() {
            var sectionStack = new StackPanel { Orientation = Orientation.Vertical };

            var headerGrid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var title = new TextBlock {
                Text = _L.ClipsTitle.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextSecondary
            };
            titleStack.Children.Add(title);

            _clipsCountBadge = new TextBlock {
                Text = "(0)",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(6, 0, 0, 0)
            };
            titleStack.Children.Add(_clipsCountBadge);

            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var openFolderBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                BorderThickness = new Thickness(0),
                Content = _L.ClipsOpenFolder
            };
            StyleModernButton(openFolderBtn, 8);
            openFolderBtn.Click += delegate { OpenClipsFolder(); };
            Grid.SetColumn(openFolderBtn, 2);
            headerGrid.Children.Add(openFolderBtn);

            sectionStack.Children.Add(headerGrid);

            _emptyNotice = new TextBlock {
                Text = string.Format(_L.ClipsEmpty, _settings.Hotkey),
                FontSize = 12,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 16, 0, 16),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            sectionStack.Children.Add(_emptyNotice);

            _clipsListPanel = new StackPanel { Orientation = Orientation.Vertical };
            sectionStack.Children.Add(_clipsListPanel);

            return sectionStack;
        }

        // ---------------------------------------------------------------------
        // VIEW 2: WIDEO I BUFOR POWTÓREK
        // ---------------------------------------------------------------------
        private ScrollViewer CreateVideoSettingsView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(CreateSettingsSectionHeader(_L.SectionHeaderVideo, _L.SectionHeaderVideoSub));

            // 1. Karta szczegółowych parametrów bufora
            var card = CreateSettingsCard();
            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            // Wybór monitora / ekranu
            var screenSelector = CreateRaycastScreenSelector();
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoScreenTitle,
                _L.VideoScreenSub,
                screenSelector
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Długość bufora
            var durStack = new StackPanel { Orientation = Orientation.Horizontal };
            int[] durations = new int[] { 15, 30, 60, 120 };
            _durationButtons.Clear();
            foreach (var d in durations) {
                int sec = d;
                var btn = CreateSegmentButton(sec + "s", _settings.ClipDuration == sec);
                btn.Click += delegate {
                    _settings.ClipDuration = sec;
                    _settings.Save();
                    UpdatePillGroup(_durationButtons, sec + "s");
                    UpdateHeroTitle();
                    RestartBufferWithNewSettings();
                };
                _durationButtons.Add(btn);
                durStack.Children.Add(btn);
            }
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoDurationTitle,
                _L.VideoDurationSub,
                durStack
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Płynność (FPS)
            var fpsStack = new StackPanel { Orientation = Orientation.Horizontal };
            int[] fpsList = new int[] { 30, 60, 120, 144 };
            _fpsButtons.Clear();
            foreach (var f in fpsList) {
                int fpsVal = f;
                var btn = CreateSegmentButton(fpsVal + " FPS", _settings.Fps == fpsVal);
                btn.Click += delegate {
                    _settings.Fps = fpsVal;
                    _settings.Save();
                    UpdatePillGroup(_fpsButtons, fpsVal + " FPS");
                    RestartBufferWithNewSettings();
                };
                _fpsButtons.Add(btn);
                fpsStack.Children.Add(btn);
            }
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoFpsTitle,
                _L.VideoFpsSub,
                fpsStack
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Bitrate wideo
            var bitStack = new StackPanel { Orientation = Orientation.Horizontal };
            string[] bitrates = new string[] { "8M", "12M", "16M" };
            _bitrateButtons.Clear();
            foreach (var b in bitrates) {
                string bitVal = b;
                var btn = CreateSegmentButton(bitVal.Replace("M", " Mbps"), _settings.Bitrate == bitVal);
                btn.Click += delegate {
                    _settings.Bitrate = bitVal;
                    _settings.Save();
                    UpdatePillGroup(_bitrateButtons, bitVal.Replace("M", " Mbps"));
                    RestartBufferWithNewSettings();
                };
                _bitrateButtons.Add(btn);
                bitStack.Children.Add(btn);
            }
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoBitrateTitle,
                _L.VideoBitrateSub,
                bitStack
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Kursor myszy
            var cursorToggle = new RaycastToggle(_settings.CaptureCursor);
            cursorToggle.CheckedChanged += delegate(bool isChecked) {
                _settings.CaptureCursor = isChecked;
                _settings.Save();
                RestartBufferWithNewSettings();
            };
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoCursorTitle,
                _L.VideoCursorSub,
                cursorToggle
            ));

            card.Child = cardStack;
            rootStack.Children.Add(card);

            // Karta enkodera i folderu
            var card2 = CreateSettingsCard();
            var card2Stack = new StackPanel { Orientation = Orientation.Vertical };

            // Hardware Encoder Status
            var gpuPill = new Border {
                Background = _theme.StatusBadgeBg,
                BorderBrush = _theme.StatusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(9, 4, 9, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock {
                    Text = "⚡ NVIDIA NVENC (Hardware)",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = _theme.StatusBadgeFg
                }
            };
            card2Stack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoGpuTitle,
                _L.VideoGpuSub,
                gpuPill
            ));
            card2Stack.Children.Add(CreateRowDivider());

            // Folder zapisu
            var folderControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _folderPathText = new TextBlock {
                Text = _settings.ClipsFolder,
                FontSize = 11.5,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                MaxWidth = 200,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            folderControls.Children.Add(_folderPathText);

            var changeFolderBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Content = _L.VideoFolderBtn
            };
            StyleModernButton(changeFolderBtn, 10);
            changeFolderBtn.Click += delegate {
                var fbd = new FolderBrowserDialog {
                    Description = _L.VideoFolderSub,
                    SelectedPath = _settings.ClipsFolder
                };
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    _settings.ClipsFolder = fbd.SelectedPath;
                    _settings.Save();
                    _folderPathText.Text = _settings.ClipsFolder;
                    RefreshClipsList();
                }
            };
            folderControls.Children.Add(changeFolderBtn);

            var openFldBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                Padding = new Thickness(12, 5, 12, 5),
                Content = "📂 " + _L.ClipsRevealBtn
            };
            StyleModernButton(openFldBtn, 10);
            openFldBtn.Click += delegate { OpenClipsFolder(); };
            folderControls.Children.Add(openFldBtn);

            card2Stack.Children.Add(CreateRaycastSettingsRow(
                _L.VideoFolderTitle,
                _L.VideoFolderSub,
                folderControls
            ));

            card2.Child = card2Stack;
            rootStack.Children.Add(card2);

            scroll.Content = rootStack;
            return scroll;
        }

        // ---------------------------------------------------------------------
        // VIEW 3: DŹWIĘK I MIKSER AUDIO
        // ---------------------------------------------------------------------
        private ScrollViewer CreateAudioSettingsView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(CreateSettingsSectionHeader(_L.SectionHeaderAudio, _L.SectionHeaderAudioSub));

            // 1. Karta mikrofonu i głośności
            var card = CreateSettingsCard();
            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            // Mikrofon
            var micSelector = CreateRaycastMicSelector();
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AudioMicDeviceTitle,
                _L.AudioMicDeviceSub,
                micSelector
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Głośność gry
            var sysVolBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _sysVolSlider = new Slider {
                Minimum = 0,
                Maximum = 150,
                Value = _settings.SystemVolumePercent,
                IsSnapToTickEnabled = true,
                TickFrequency = 5,
                Width = 140,
                Height = 22,
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _sysVolLabel = new TextBlock {
                Text = _settings.SystemVolumePercent + "%",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AccentCoral,
                Width = 46,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            _sysVolSlider.ValueChanged += delegate {
                int val = (int)_sysVolSlider.Value;
                _settings.SystemVolumePercent = val;
                _sysVolLabel.Text = val + "%";
                _engine.UpdateAudioVolumes(_settings.SystemVolumePercent, _settings.MicVolumePercent);
            };
            _sysVolSlider.PreviewMouseUp += delegate { _settings.Save(); };
            sysVolBox.Children.Add(_sysVolSlider);
            sysVolBox.Children.Add(_sysVolLabel);

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AudioSysVolTitle,
                _L.AudioSysVolSub,
                sysVolBox
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Głośność mikrofonu
            var micVolBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _micVolSlider = new Slider {
                Minimum = 0,
                Maximum = 150,
                Value = _settings.MicVolumePercent,
                IsSnapToTickEnabled = true,
                TickFrequency = 5,
                Width = 140,
                Height = 22,
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _micVolLabel = new TextBlock {
                Text = _settings.MicVolumePercent + "%",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AccentCoral,
                Width = 46,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            _micVolSlider.ValueChanged += delegate {
                int val = (int)_micVolSlider.Value;
                _settings.MicVolumePercent = val;
                _micVolLabel.Text = val + "%";
                _engine.UpdateAudioVolumes(_settings.SystemVolumePercent, _settings.MicVolumePercent);
            };
            _micVolSlider.PreviewMouseUp += delegate { _settings.Save(); };
            micVolBox.Children.Add(_micVolSlider);
            micVolBox.Children.Add(_micVolLabel);

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AudioMicVolTitle,
                _L.AudioMicVolSub,
                micVolBox
            ));

            card.Child = cardStack;
            rootStack.Children.Add(card);

            // 2. Karta personalizacji dźwięku powtórki
            var soundCard = CreateSettingsCard();
            var soundStack = new StackPanel { Orientation = Orientation.Vertical };

            // Toggle włącz/wyłącz dźwięk
            var soundToggle = new RaycastToggle(_settings.PlaySound);
            soundToggle.CheckedChanged += delegate(bool isChecked) {
                _settings.PlaySound = isChecked;
                _settings.Save();
            };
            soundStack.Children.Add(CreateRaycastSettingsRow(
                _L.AudioPlaySoundTitle,
                _L.AudioPlaySoundSub,
                soundToggle
            ));
            soundStack.Children.Add(CreateRowDivider());

            // Presety dźwiękowe (pełna szerokość karty)
            var presetBlock = new StackPanel {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 8, 0, 8)
            };

            var presetText = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
            presetText.Children.Add(new TextBlock {
                Text = _L.AudioPresetTitle,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = _theme.TextPrimary
            });
            presetText.Children.Add(new TextBlock {
                Text = _L.AudioPresetSub,
                FontSize = 11,
                Foreground = _theme.TextSecondary,
                Margin = new Thickness(0, 2, 0, 0)
            });
            presetBlock.Children.Add(presetText);

            var presetsWrap = new WrapPanel {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };
            string[] presetLabels = new string[] { _L.SoundPresetDefault, _L.SoundPresetShutter, _L.SoundPresetDing, _L.SoundPresetRetro, _L.SoundPresetCustom };
            string[] presetKeys = new string[] { "Default", "Shutter", "Ding", "Retro", "Custom" };
            _soundPresetButtons.Clear();

            for (int i = 0; i < presetKeys.Length; i++) {
                string key = presetKeys[i];
                string label = presetLabels[i];
                bool isSelected = (_settings.SoundPreset == key);
                var btn = CreateSegmentButton(label, isSelected);
                btn.Click += delegate {
                    _settings.SoundPreset = key;
                    _settings.Save();
                    SoundEffect.Configure(_settings);
                    UpdatePillGroup(_soundPresetButtons, label);
                    SoundEffect.PlayPreview(_settings.SoundPreset, _settings.CustomSoundPath);
                };
                _soundPresetButtons.Add(btn);
                presetsWrap.Children.Add(btn);
            }
            presetBlock.Children.Add(presetsWrap);
            soundStack.Children.Add(presetBlock);
            soundStack.Children.Add(CreateRowDivider());

            // Własny plik dźwiękowy
            var customFileRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _customSoundPathText = new TextBlock {
                Text = string.IsNullOrEmpty(_settings.CustomSoundPath) ? _L.SoundCustomEmpty : Path.GetFileName(_settings.CustomSoundPath),
                FontSize = 11.5,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                MaxWidth = 180,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            customFileRow.Children.Add(_customSoundPathText);

            var pickSoundBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                Padding = new Thickness(14, 5, 14, 5),
                Content = _L.AudioCustomBtn
            };
            StyleModernButton(pickSoundBtn, 10);
            pickSoundBtn.Click += delegate {
                var ofd = new OpenFileDialog {
                    Title = _L.AudioCustomTitle,
                    Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*"
                };
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    _settings.CustomSoundPath = ofd.FileName;
                    _settings.SoundPreset = "Custom";
                    _settings.Save();
                    _customSoundPathText.Text = Path.GetFileName(ofd.FileName);
                    SoundEffect.Configure(_settings);
                    UpdatePillGroup(_soundPresetButtons, _L.SoundPresetCustom);
                    SoundEffect.PlayPreview("Custom", ofd.FileName);
                }
            };
            customFileRow.Children.Add(pickSoundBtn);

            soundStack.Children.Add(CreateRaycastSettingsRow(
                _L.AudioCustomTitle,
                _L.AudioCustomSub,
                customFileRow
            ));

            soundCard.Child = soundStack;
            rootStack.Children.Add(soundCard);

            scroll.Content = rootStack;
            return scroll;
        }

        // ---------------------------------------------------------------------
        // VIEW 4: SKRÓTY KLAWISZOWE
        // ---------------------------------------------------------------------
        private ScrollViewer CreateHotkeysSettingsView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(CreateSettingsSectionHeader(_L.SectionHeaderHotkeys, _L.SectionHeaderHotkeysSub));

            var card = CreateSettingsCard();
            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            var hkStack = new StackPanel { Orientation = Orientation.Horizontal };
            string[] hotkeys = new string[] { "F8", "F9", "F10", "Alt+C" };
            _hotkeyButtons.Clear();
            foreach (var h in hotkeys) {
                string hk = h;
                var btn = CreateSegmentButton(hk, _settings.Hotkey == hk);
                btn.Click += delegate {
                    _settings.Hotkey = hk;
                    _settings.Save();
                    UpdatePillGroup(_hotkeyButtons, hk);
                    UpdateHeroTitle();
                    RegisterGlobalHotkey();
                };
                _hotkeyButtons.Add(btn);
                hkStack.Children.Add(btn);
            }
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.HotkeyTitle,
                _L.HotkeySub,
                hkStack
            ));
            cardStack.Children.Add(CreateRowDivider());

            var activeIndicator = new Border {
                Background = _theme.StatusBadgeBg,
                BorderBrush = _theme.StatusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(9, 4, 9, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock {
                    Text = _L.HotkeyBadgeText,
                    FontSize = 11.5,
                    FontWeight = FontWeights.Medium,
                    Foreground = _theme.StatusBadgeFg
                }
            };
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.HotkeyGlobalTitle,
                _L.HotkeyGlobalSub,
                activeIndicator
            ));

            card.Child = cardStack;
            rootStack.Children.Add(card);

            scroll.Content = rootStack;
            return scroll;
        }

        // ---------------------------------------------------------------------
        // VIEW 5: SYSTEM I AUTOSTART
        // ---------------------------------------------------------------------
        private ScrollViewer CreateSystemSettingsView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(CreateSettingsSectionHeader(_L.SectionHeaderSystem, _L.SectionHeaderSystemSub));

            // 1. Karta wyglądu i nakładki HUD
            var themeCard = CreateSettingsCard();
            var themeCardStack = new StackPanel { Orientation = Orientation.Vertical };

            // Motyw aplikacji (Ciemny / Jasny)
            var themeBtnStack = new StackPanel { Orientation = Orientation.Horizontal };
            _themeButtons.Clear();

            var darkBtn = CreateSegmentButton("🌙 Dark", _theme.IsDark);
            darkBtn.Click += delegate { ApplyTheme(true); };
            _themeButtons.Add(darkBtn);
            themeBtnStack.Children.Add(darkBtn);

            var lightBtn = CreateSegmentButton("☀️ Light", !_theme.IsDark);
            lightBtn.Click += delegate { ApplyTheme(false); };
            _themeButtons.Add(lightBtn);
            themeBtnStack.Children.Add(lightBtn);

            themeCardStack.Children.Add(CreateRaycastSettingsRow(
                _L.SystemThemeTitle,
                _L.SystemThemeSub,
                themeBtnStack
            ));
            themeCardStack.Children.Add(CreateRowDivider());

            // Powiadomienia HUD Overlay
            var hudToggle = new RaycastToggle(_settings.ShowNotification);
            hudToggle.CheckedChanged += delegate(bool isChecked) {
                _settings.ShowNotification = isChecked;
                _settings.Save();
            };
            themeCardStack.Children.Add(CreateRaycastSettingsRow(
                _L.SystemHudTitle,
                _L.SystemHudSub,
                hudToggle
            ));

            themeCard.Child = themeCardStack;
            rootStack.Children.Add(themeCard);

            // 2. Karta autostartu i kontroli bufora
            var card = CreateSettingsCard();
            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            // Autostart z Windows
            var autostartToggle = new RaycastToggle(AutostartHelper.IsAutostartEnabled());
            autostartToggle.CheckedChanged += delegate(bool isChecked) {
                AutostartHelper.SetAutostart(isChecked);
                _settings.AutoStart = isChecked;
                _settings.Save();
            };
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.SystemAutostartTitle,
                _L.SystemAutostartSub,
                autostartToggle
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Start w zasobniku
            var startMinToggle = new RaycastToggle(_settings.StartMinimized);
            startMinToggle.CheckedChanged += delegate(bool isChecked) {
                _settings.StartMinimized = isChecked;
                _settings.Save();
            };
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.SystemStartMinTitle,
                _L.SystemStartMinSub,
                startMinToggle
            ));
            cardStack.Children.Add(CreateRowDivider());

            // Kontrola bufora
            _toggleBufferBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                Padding = new Thickness(14, 6, 14, 6),
                Content = _engine.IsRecording ? _L.TrayPauseBuffer : _L.TrayResumeBuffer
            };
            StyleModernButton(_toggleBufferBtn, 10);
            _toggleBufferBtn.Click += delegate {
                if (_engine.IsRecording) {
                    _engine.Stop();
                } else {
                    _engine.Start();
                }
            };
            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.SystemBufferActivityTitle,
                _L.SystemBufferActivitySub,
                _toggleBufferBtn
            ));

            card.Child = cardStack;
            rootStack.Children.Add(card);

            scroll.Content = rootStack;
            return scroll;
        }

        // ---------------------------------------------------------------------
        // VIEW 6: O PROGRAMIE (ABOUT)
        // ---------------------------------------------------------------------
        private ScrollViewer CreateAboutView() {
            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 10, 24, 16)
            };

            var rootStack = new StackPanel { Orientation = Orientation.Vertical };
            rootStack.Children.Add(CreateSettingsSectionHeader(_L.NavAbout, _L.AboutVersionSub));

            var card = CreateSettingsCard();
            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AboutVersionTitle,
                _L.AboutVersionSub,
                new TextBlock { Text = "Rewind v0.1", FontWeight = FontWeights.Bold, Foreground = _theme.TextPrimary }
            ));
            cardStack.Children.Add(CreateRowDivider());

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AboutGpuTitle,
                _L.AboutGpuSub,
                new TextBlock { Text = "NVIDIA NVENC / QuickSync / AMF", Foreground = _theme.StatusBadgeFg, FontWeight = FontWeights.SemiBold }
            ));
            cardStack.Children.Add(CreateRowDivider());

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AboutAudioTitle,
                _L.AboutAudioSub,
                new TextBlock { Text = "WASAPI Studio 48kHz", Foreground = _theme.TextPrimary }
            ));
            cardStack.Children.Add(CreateRowDivider());

            cardStack.Children.Add(CreateRaycastSettingsRow(
                _L.AboutFootprintTitle,
                _L.AboutFootprintSub,
                new TextBlock { Text = "< 0.5% CPU • ~80 MB RAM", Foreground = _theme.AccentCoral, FontWeight = FontWeights.SemiBold }
            ));

            card.Child = cardStack;
            rootStack.Children.Add(card);

            scroll.Content = rootStack;
            return scroll;
        }

        // ---------------------------------------------------------------------
        // RAYCAST UI HELPERS
        // ---------------------------------------------------------------------
        private FrameworkElement CreateSettingsSectionHeader(string title, string subtitle) {
            var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            var titleTb = new TextBlock {
                Text = title,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 3)
            };
            stack.Children.Add(titleTb);

            var subTb = new TextBlock {
                Text = subtitle,
                FontSize = 12,
                Foreground = _theme.TextSecondary
            };
            stack.Children.Add(subTb);

            return stack;
        }

        private Border CreateSettingsCard() {
            return new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private FrameworkElement CreateRaycastSettingsRow(string label, string desc, FrameworkElement control) {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 220 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            var labelTb = new TextBlock {
                Text = label,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = _theme.TextPrimary
            };
            textStack.Children.Add(labelTb);

            var descTb = new TextBlock {
                Text = desc,
                FontSize = 11,
                Foreground = _theme.TextSecondary,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            textStack.Children.Add(descTb);

            Grid.SetColumn(textStack, 0);
            grid.Children.Add(textStack);

            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);

            return grid;
        }

        private FrameworkElement CreateRowDivider() {
            return new Border {
                Height = 1,
                Background = _theme.RowDivider,
                Margin = new Thickness(0, 6, 0, 6)
            };
        }

        private Button CreateSegmentButton(string text, bool isActive) {
            var btn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 11.5,
                BorderThickness = new Thickness(1),
                Content = text
            };
            StyleModernButton(btn, 12);
            ApplySegmentStyle(btn, isActive);

            btn.MouseEnter += delegate {
                if (btn.FontWeight != FontWeights.SemiBold) {
                    btn.Background = _theme.ButtonHoverBg;
                    btn.BorderBrush = _theme.ButtonBorder;
                    btn.Foreground = _theme.TextPrimary;
                }
            };
            btn.MouseLeave += delegate {
                if (btn.FontWeight != FontWeights.SemiBold) {
                    btn.Background = _theme.PillUnselectedBg;
                    btn.BorderBrush = _theme.PillUnselectedBorder;
                    btn.Foreground = _theme.PillUnselectedFg;
                }
            };
            return btn;
        }

        private void ApplySegmentStyle(Button btn, bool isActive) {
            if (isActive) {
                btn.Background = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(44, 44, 44)) : new SolidColorBrush(Color.FromRgb(255, 255, 255));
                btn.BorderBrush = _theme.IsDark ? new SolidColorBrush(Color.FromRgb(68, 68, 68)) : new SolidColorBrush(Color.FromRgb(200, 202, 210));
                btn.Foreground = _theme.TextPrimary;
                btn.FontWeight = FontWeights.SemiBold;
            } else {
                btn.Background = _theme.PillUnselectedBg;
                btn.BorderBrush = _theme.PillUnselectedBorder;
                btn.Foreground = _theme.PillUnselectedFg;
                btn.FontWeight = FontWeights.Normal;
            }
        }

        private void UpdatePillGroup(List<Button> buttons, string selectedText) {
            foreach (var b in buttons) {
                ApplySegmentStyle(b, b.Content.ToString() == selectedText);
            }
        }

        private void UpdateHeroTitle() {
            if (_heroTitle != null) {
                _heroTitle.Text = string.Format(_L.HeroTitleFormat, _settings.ClipDuration);
            }
            if (_heroClipBtn != null) {
                var border = _heroClipBtn.Content as Border;
                if (border != null) {
                    var tb = border.Child as TextBlock;
                    if (tb != null) {
                        tb.Text = string.Format("⚡ {0} ({1})", _L.HeroButton.Replace("✦  ", "").Replace("✦ ", ""), _settings.Hotkey);
                    }
                }
            }
            if (_heroHotkeyBadge != null) {
                _heroHotkeyBadge.Text = _L.HeroHotkeyPrefix + _settings.Hotkey;
            }
            if (_sidebarInfoSubtext != null) {
                _sidebarInfoSubtext.Text = string.Format(_L.SidebarInfoFormat, _settings.Hotkey, _settings.ClipDuration);
            }
            if (_notifyIcon != null) {
                _notifyIcon.Text = string.Format("Rewind - [{0}]", _settings.Hotkey);
            }
        }

        // ---------------------------------------------------------------------
        // RAYCAST POPUP SCREEN / MONITOR SELECTOR
        // ---------------------------------------------------------------------
        private FrameworkElement CreateRaycastScreenSelector() {
            var grid = new Grid();

            var selectBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Height = 32,
                MinWidth = 230,
                Background = _theme.InputBg,
                BorderBrush = _theme.InputBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 0, 10, 0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            StyleModernButton(selectBtn, 10);

            var btnContent = new Grid();
            btnContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _screenDropdownText = new TextBlock {
                Text = GetSelectedScreenFriendlyName(),
                FontSize = 12,
                Foreground = _theme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 190
            };
            Grid.SetColumn(_screenDropdownText, 0);
            btnContent.Children.Add(_screenDropdownText);

            var chevron = new TextBlock {
                Text = "▾",
                FontSize = 12,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            Grid.SetColumn(chevron, 1);
            btnContent.Children.Add(chevron);

            selectBtn.Content = btnContent;

            var popup = new Popup {
                PlacementTarget = selectBtn,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            var popupBorder = new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                MinWidth = 250
            };

            var itemsStack = new StackPanel { Orientation = Orientation.Vertical };

            Action refreshItems = delegate {
                itemsStack.Children.Clear();
                var screens = System.Windows.Forms.Screen.AllScreens;

                for (int i = 0; i < screens.Length; i++) {
                    int index = i;
                    var s = screens[i];
                    bool isSelected = IsScreenSelected(s, index);

                    var itemBtn = new Button {
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Height = 32,
                        Background = isSelected ? _theme.AccentCoralSoft : System.Windows.Media.Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 0, 8, 0),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    StyleModernButton(itemBtn, 8);

                    var itemGrid = new Grid();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var checkTb = new TextBlock {
                        Text = isSelected ? "✓" : "",
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Foreground = _theme.AccentCoral,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(checkTb, 0);
                    itemGrid.Children.Add(checkTb);

                    string screenLabel = FormatScreenLabel(s, index);
                    var nameTb = new TextBlock {
                        Text = screenLabel,
                        FontSize = 12,
                        Foreground = isSelected ? _theme.TextPrimary : _theme.TextSecondary,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(nameTb, 1);
                    itemGrid.Children.Add(nameTb);

                    itemBtn.Content = itemGrid;

                    itemBtn.MouseEnter += delegate {
                        itemBtn.Background = _theme.ButtonHoverBg;
                        nameTb.Foreground = _theme.TextPrimary;
                    };
                    itemBtn.MouseLeave += delegate {
                        itemBtn.Background = isSelected ? _theme.AccentCoralSoft : System.Windows.Media.Brushes.Transparent;
                        nameTb.Foreground = isSelected ? _theme.TextPrimary : _theme.TextSecondary;
                    };

                    itemBtn.Click += delegate {
                        _settings.SelectedDisplayIndex = index;
                        _settings.SelectedDisplayDevice = s.DeviceName;
                        _settings.Save();
                        _screenDropdownText.Text = FormatScreenLabel(s, index);
                        popup.IsOpen = false;
                        RestartBufferWithNewSettings();
                    };

                    itemsStack.Children.Add(itemBtn);
                }
            };

            selectBtn.Click += delegate {
                refreshItems();
                popup.IsOpen = !popup.IsOpen;
            };

            popupBorder.Child = itemsStack;
            popup.Child = popupBorder;

            grid.Children.Add(selectBtn);
            grid.Children.Add(popup);
            return grid;
        }

        private string FormatScreenLabel(System.Windows.Forms.Screen s, int index) {
            string primaryBadge = s.Primary ? (" (" + _L.ScreenPrimaryBadge + ")") : "";
            return string.Format("{0} {1}: {2}x{3}{4}", _L.ScreenLabel, index + 1, s.Bounds.Width, s.Bounds.Height, primaryBadge);
        }

        private string GetSelectedScreenFriendlyName() {
            try {
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (screens != null && screens.Length > 0) {
                    for (int i = 0; i < screens.Length; i++) {
                        if (IsScreenSelected(screens[i], i)) {
                            return FormatScreenLabel(screens[i], i);
                        }
                    }
                    return FormatScreenLabel(screens[0], 0);
                }
            } catch { }
            return _L.ScreenPrimaryDefault;
        }

        private bool IsScreenSelected(System.Windows.Forms.Screen s, int index) {
            if (!string.IsNullOrEmpty(_settings.SelectedDisplayDevice)) {
                return string.Equals(s.DeviceName, _settings.SelectedDisplayDevice, StringComparison.OrdinalIgnoreCase);
            }
            return _settings.SelectedDisplayIndex == index || (s.Primary && _settings.SelectedDisplayIndex <= 0);
        }

        // ---------------------------------------------------------------------
        // RAYCAST POPUP MIC SELECTOR
        // ---------------------------------------------------------------------
        private FrameworkElement CreateRaycastMicSelector() {
            var grid = new Grid();

            var selectBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Height = 32,
                MinWidth = 230,
                Background = _theme.InputBg,
                BorderBrush = _theme.InputBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 0, 10, 0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            StyleModernButton(selectBtn, 10);

            var btnContent = new Grid();
            btnContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _micDropdownText = new TextBlock {
                Text = GetSelectedMicFriendlyName(),
                FontSize = 12,
                Foreground = _theme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 190
            };
            Grid.SetColumn(_micDropdownText, 0);
            btnContent.Children.Add(_micDropdownText);

            var chevron = new TextBlock {
                Text = "▾",
                FontSize = 12,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            Grid.SetColumn(chevron, 1);
            btnContent.Children.Add(chevron);

            selectBtn.Content = btnContent;

            var popup = new Popup {
                PlacementTarget = selectBtn,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            var popupBorder = new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                MinWidth = 250
            };

            var itemsStack = new StackPanel { Orientation = Orientation.Vertical };

            Action refreshItems = delegate {
                itemsStack.Children.Clear();
                var devices = WasapiLoopbackServer.GetMicrophoneDevices();

                foreach (var device in devices) {
                    var d = device;
                    bool isSelected = (d.Id == _settings.SelectedMicId);

                    var itemBtn = new Button {
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Height = 32,
                        Background = isSelected ? _theme.AccentCoralSoft : System.Windows.Media.Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 0, 8, 0),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };
                    StyleModernButton(itemBtn, 8);

                    var itemGrid = new Grid();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var checkTb = new TextBlock {
                        Text = isSelected ? "✓" : "",
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Foreground = _theme.AccentCoral,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(checkTb, 0);
                    itemGrid.Children.Add(checkTb);

                    var nameTb = new TextBlock {
                        Text = d.Name,
                        FontSize = 12,
                        Foreground = isSelected ? _theme.TextPrimary : _theme.TextSecondary,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(nameTb, 1);
                    itemGrid.Children.Add(nameTb);

                    itemBtn.Content = itemGrid;

                    itemBtn.MouseEnter += delegate {
                        itemBtn.Background = _theme.ButtonHoverBg;
                        nameTb.Foreground = _theme.TextPrimary;
                    };
                    itemBtn.MouseLeave += delegate {
                        itemBtn.Background = (d.Id == _settings.SelectedMicId) ? _theme.AccentCoralSoft : System.Windows.Media.Brushes.Transparent;
                        nameTb.Foreground = (d.Id == _settings.SelectedMicId) ? _theme.TextPrimary : _theme.TextSecondary;
                    };

                    itemBtn.Click += delegate {
                        _settings.SelectedMicId = d.Id;
                        _settings.Save();
                        _micDropdownText.Text = d.Name;
                        popup.IsOpen = false;
                        RestartBufferWithNewSettings();
                    };

                    itemsStack.Children.Add(itemBtn);
                }
            };

            selectBtn.Click += delegate {
                refreshItems();
                popup.IsOpen = !popup.IsOpen;
            };

            popupBorder.Child = itemsStack;
            popup.Child = popupBorder;

            grid.Children.Add(selectBtn);
            grid.Children.Add(popup);
            return grid;
        }

        private string GetSelectedMicFriendlyName() {
            try {
                var devices = WasapiLoopbackServer.GetMicrophoneDevices();
                foreach (var d in devices) {
                    if (d.Id == _settings.SelectedMicId) return d.Name;
                }
            } catch { }
            return _L.AudioMicDefault;
        }

        // ---------------------------------------------------------------------
        // GORGEOUS TASKBAR TRAY MENU SETUP
        // ---------------------------------------------------------------------
        private void InitTrayIcon() {
            if (_notifyIcon != null) {
                try {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                } catch { }
                _notifyIcon = null;
            }

            _notifyIcon = new NotifyIcon {
                Text = string.Format("Rewind [{0}]", _settings.Hotkey),
                Icon = LoadAppIcon(),
                Visible = true
            };

            var menu = new ContextMenuStrip {
                ShowImageMargin = false,
                ShowCheckMargin = false,
                Renderer = new RaycastMenuRenderer(),
                Padding = new System.Windows.Forms.Padding(4),
                BackColor = System.Drawing.Color.FromArgb(16, 16, 16),
                Font = new Font("Segoe UI Variable Text", 9.5f, System.Drawing.FontStyle.Regular)
            };

            // 1. Header Banner Item (Rewind v0.1)
            var header = new ToolStripMenuItem(_L.TrayHeader) {
                Tag = "header",
                Enabled = false,
                Font = new Font("Segoe UI Variable Text", 10f, System.Drawing.FontStyle.Bold)
            };
            menu.Items.Add(header);

            _trayStatusItem = new ToolStripMenuItem(_engine.IsRecording ? _L.TrayBufferActive : _L.TrayBufferPaused) {
                Tag = "status",
                Enabled = false,
                Font = new Font("Segoe UI Variable Text", 8.5f, System.Drawing.FontStyle.Regular)
            };
            menu.Items.Add(_trayStatusItem);

            menu.Items.Add(new ToolStripSeparator());

            // 2. Action: Save Clip Now
            var clipItem = new ToolStripMenuItem(_L.TraySaveClip, null, delegate {
                _engine.SaveClip();
            }) {
                ShortcutKeyDisplayString = "[" + _settings.Hotkey + "]",
                Font = new Font("Segoe UI Variable Text", 9.5f, System.Drawing.FontStyle.Bold)
            };
            menu.Items.Add(clipItem);

            // 3. Action: Pause / Resume Buffer
            _trayBufferToggleItem = new ToolStripMenuItem(_engine.IsRecording ? _L.TrayPauseBuffer : _L.TrayResumeBuffer, null, delegate {
                if (_engine.IsRecording) {
                    _engine.Stop();
                } else {
                    _engine.Start();
                }
            });
            menu.Items.Add(_trayBufferToggleItem);

            menu.Items.Add(new ToolStripSeparator());

            // 4. Open Settings
            var settingsItem = new ToolStripMenuItem(_L.TraySettings, null, delegate {
                ShowAndActivate();
            });
            menu.Items.Add(settingsItem);

            // 5. Open Clips Folder
            var folderItem = new ToolStripMenuItem(_L.TrayOpenFolder, null, delegate {
                OpenClipsFolder();
            });
            menu.Items.Add(folderItem);

            menu.Items.Add(new ToolStripSeparator());

            // 6. Exit
            var exitItem = new ToolStripMenuItem(_L.TrayExit, null, delegate {
                _isExiting = true;
                _engine.Stop();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Application.Current.Shutdown();
            });
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;

            // Single left click restores window; double click also restores window
            _notifyIcon.MouseClick += delegate(object sender, System.Windows.Forms.MouseEventArgs e) {
                if (e.Button == MouseButtons.Left) {
                    ToggleWindowVisibility();
                }
            };
        }

        public void ShowAndActivate() {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_hwnd != IntPtr.Zero) {
                NativeMethods.SetForegroundWindow(_hwnd);
            }
        }

        public void ToggleWindowVisibility() {
            if (IsVisible && WindowState != WindowState.Minimized) {
                Hide();
            } else {
                ShowAndActivate();
            }
        }

        public void InitializeSystemIntegration(bool startMinimized) {
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
            _hwnd = helper.Handle;

            NativeMethods.ApplyModernDwmAttributes(_hwnd);

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            if (_hwndSource != null) {
                _hwndSource.AddHook(HwndHook);
            }

            RegisterGlobalHotkey();
            RefreshClipsList();

            if (_settings.BufferActive) {
                _engine.Start();
            }

            if (!startMinimized) {
                ShowAndActivate();
            } else {
                WindowState = WindowState.Minimized;
                Hide();
                if (_notifyIcon != null) {
                    _notifyIcon.ShowBalloonTip(
                        2500,
                        _L.BalloonTitle,
                        string.Format(_L.BalloonSub, _settings.Hotkey),
                        ToolTipIcon.Info
                    );
                }
            }
        }

        private void RegisterGlobalHotkey() {
            if (_hwnd == IntPtr.Zero) return;
            NativeMethods.UnregisterHotKey(_hwnd, NativeMethods.HOTKEY_ID);

            uint mod, vk;
            NativeMethods.ParseHotkey(_settings.Hotkey, out mod, out vk);
            NativeMethods.RegisterHotKey(_hwnd, NativeMethods.HOTKEY_ID, mod, vk);

            if (_notifyIcon != null) {
                _notifyIcon.Text = string.Format("Rewind [{0}]", _settings.Hotkey);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == NativeMethods.WM_HOTKEY) {
                if (wParam.ToInt32() == NativeMethods.HOTKEY_ID) {
                    _engine.SaveClip();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!_isExiting) {
                e.Cancel = true;
                Hide();
                if (_notifyIcon != null) {
                    _notifyIcon.ShowBalloonTip(
                        2000,
                        _L.BalloonTitle,
                        string.Format(_L.BalloonSub, _settings.Hotkey),
                        ToolTipIcon.Info
                    );
                }
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                Hide();
                e.Handled = true;
            } else if (e.Key == Key.F8 && _settings.Hotkey == "F8") {
                _engine.SaveClip();
                e.Handled = true;
            }
        }

        private void HookEngineEvents() {
            _engine.StatusChanged += delegate(bool recording, string text) {
                Dispatcher.Invoke(new Action(delegate {
                    if (recording) {
                        _sidebarStatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        _sidebarStatusText.Text = _L.SidebarStatusActive;
                        _sidebarStatusText.Foreground = new SolidColorBrush(Color.FromRgb(209, 250, 229));
                        if (_toggleBufferBtn != null) _toggleBufferBtn.Content = _L.TrayPauseBuffer;
                        if (_trayBufferToggleItem != null) _trayBufferToggleItem.Text = _L.TrayPauseBuffer;
                        if (_trayStatusItem != null) {
                            _trayStatusItem.Text = _L.TrayBufferActive;
                            _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
                        }
                    } else {
                        _sidebarStatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        _sidebarStatusText.Text = _L.SidebarStatusPaused;
                        _sidebarStatusText.Foreground = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                        if (_toggleBufferBtn != null) _toggleBufferBtn.Content = _L.TrayResumeBuffer;
                        if (_trayBufferToggleItem != null) _trayBufferToggleItem.Text = _L.TrayResumeBuffer;
                        if (_trayStatusItem != null) {
                            _trayStatusItem.Text = _L.TrayBufferPaused;
                            _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(245, 158, 11);
                        }
                    }
                }));
            };

            _engine.ClipSaved += delegate(string path, int duration) {
                Dispatcher.Invoke(new Action(delegate {
                    _lastSavedClipPath = path;
                    RefreshClipsList();

                    string fileName = Path.GetFileName(path);
                    _toastText.Text = string.Format("🎬 {0}: {1} ({2}s)", _L.ClipsTitle, fileName, duration);
                    _toastBanner.Visibility = Visibility.Visible;

                    if (_settings.ShowNotification) {
                        HudNotificationWindow.ShowToast(fileName, duration, path, _theme, _L);
                    }

                    if (_notifyIcon != null) {
                        _notifyIcon.ShowBalloonTip(
                            3000,
                            string.Format(_L.HudTitleFormat, duration),
                            string.Format("{0} ({1}s)", fileName, duration),
                            ToolTipIcon.Info
                        );
                    }
                }));
            };

            _engine.ErrorOccurred += delegate(string error) {
                Dispatcher.Invoke(new Action(delegate {
                    if (_notifyIcon != null) {
                        _notifyIcon.ShowBalloonTip(3000, "Rewind Info", error, ToolTipIcon.Warning);
                    }
                }));
            };
        }

        private void RestartBufferWithNewSettings() {
            ThreadPool.QueueUserWorkItem(delegate {
                if (_engine.IsRecording) {
                    _engine.Stop();
                    _engine.Start();
                }
            });
        }

        public void RefreshClipsList() {
            string clipsDir = _settings.ClipsFolder;
            if (!Directory.Exists(clipsDir)) {
                try { Directory.CreateDirectory(clipsDir); } catch { }
            }

            var dir = new DirectoryInfo(clipsDir);
            var files = dir.GetFiles("*.mp4")
                           .OrderByDescending(f => f.LastWriteTime)
                           .Take(25)
                           .ToList();

            if (_clipsCountBadge != null) _clipsCountBadge.Text = string.Format("({0})", files.Count);
            if (_clipsListPanel == null) return;
            _clipsListPanel.Children.Clear();

            if (files.Count == 0) {
                if (_emptyNotice != null) _emptyNotice.Visibility = Visibility.Visible;
                return;
            }
            if (_emptyNotice != null) _emptyNotice.Visibility = Visibility.Collapsed;

            foreach (var f in files) {
                _clipsListPanel.Children.Add(CreateClipItemCard(f));
            }
        }

        private FrameworkElement CreateClipItemCard(FileInfo file) {
            var card = new Border {
                Background = _theme.CardBg,
                BorderBrush = _theme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(8),
                Background = _theme.AccentCoralSoft,
                Margin = new Thickness(0, 0, 14, 0),
                Child = new TextBlock {
                    Text = "▶",
                    Foreground = _theme.AccentCoral,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nameText = new TextBlock {
                Text = file.Name,
                FontWeight = FontWeights.Medium,
                FontSize = 13,
                Foreground = _theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textStack.Children.Add(nameText);

            double mb = file.Length / (1024.0 * 1024.0);
            string timeStr = file.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss");
            var metaText = new TextBlock {
                Text = string.Format("{0:0.1} MB • {1}", mb, timeStr),
                FontSize = 11.5,
                Foreground = _theme.TextSecondary,
                Margin = new Thickness(0, 2, 0, 0)
            };
            textStack.Children.Add(metaText);

            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var actionStack = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var playBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Content = _L.ClipsPlay
            };
            StyleModernButton(playBtn, 10);
            playBtn.Click += delegate {
                try { Process.Start(new ProcessStartInfo(file.FullName) { UseShellExecute = true }); } catch { }
            };
            actionStack.Children.Add(playBtn);

            var revealBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = _theme.ButtonBg,
                BorderBrush = _theme.ButtonBorder,
                BorderThickness = new Thickness(1),
                Foreground = _theme.TextPrimary,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Content = "📂 " + _L.ClipsRevealBtn
            };
            StyleModernButton(revealBtn, 10);
            revealBtn.Click += delegate {
                try { Process.Start("explorer.exe", string.Format("/select,\"{0}\"", file.FullName)); } catch { }
            };
            actionStack.Children.Add(revealBtn);

            var delBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontSize = 12,
                Padding = new Thickness(8, 5, 8, 5),
                BorderThickness = new Thickness(0),
                Content = "🗑"
            };
            StyleModernButton(delBtn, 8);
            delBtn.Click += delegate {
                if (MessageBox.Show(string.Format(_L.ClipsDeleteConfirm, file.Name), _L.ClipsDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                    try {
                        file.Delete();
                        RefreshClipsList();
                    } catch { }
                }
            };
            actionStack.Children.Add(delBtn);

            Grid.SetColumn(actionStack, 2);
            grid.Children.Add(actionStack);

            card.Child = grid;
            return card;
        }

        private void OpenClipsFolder() {
            try {
                string clipsDir = _settings.ClipsFolder;
                if (!Directory.Exists(clipsDir)) {
                    Directory.CreateDirectory(clipsDir);
                }
                Process.Start("explorer.exe", clipsDir);
            } catch { }
        }
    }
}
