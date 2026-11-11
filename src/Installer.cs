using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using ProgressBar = System.Windows.Controls.ProgressBar;
using TextBox = System.Windows.Controls.TextBox;

namespace RewindInstaller {
    public class Program {
        [STAThread]
        public static void Main(string[] args) {
            bool isUninstall = false;
            if (args != null) {
                foreach (var a in args) {
                    if (a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("-uninstall", StringComparison.OrdinalIgnoreCase)) {
                        isUninstall = true;
                        break;
                    }
                }
            }

            var app = new Application();
            var win = new InstallerWindow(isUninstall);
            app.Run(win);
        }
    }

    public class InstallerStrings {
        public string LangName { get; set; }
        public string Flag { get; set; }
        public string TitleSetup { get; set; }
        public string TitleUninstall { get; set; }
        public string HeaderInstallTitle { get; set; }
        public string HeaderInstallSub { get; set; }
        public string HeaderUninstallTitle { get; set; }
        public string HeaderUninstallSub { get; set; }
        public string PathLabel { get; set; }
        public string BrowseBtn { get; set; }
        public string BrowseDialogDesc { get; set; }
        public string OptLabel { get; set; }
        public string CbDesktop { get; set; }
        public string CbStartMenu { get; set; }
        public string CbAutostart { get; set; }
        public string CbLaunchNow { get; set; }
        public string UninstallConfirm { get; set; }
        public string ActionInstall { get; set; }
        public string ActionUninstall { get; set; }
        public string ActionFinish { get; set; }
        public string ActionClose { get; set; }
        public string ProgressTitleInstall { get; set; }
        public string ProgressTitleUninstall { get; set; }
        public string ProgressClosing { get; set; }
        public string ProgressDirectory { get; set; }
        public string ProgressCopying { get; set; }
        public string ProgressShortcuts { get; set; }
        public string ProgressRegistering { get; set; }
        public string ProgressDoneInstall { get; set; }
        public string ProgressClosingUninstall { get; set; }
        public string ProgressRemovingShortcuts { get; set; }
        public string ProgressRemovingRegistry { get; set; }
        public string ProgressRemovingFiles { get; set; }
        public string ProgressDoneUninstall { get; set; }
        public string FinishInstallTitle { get; set; }
        public string FinishInstallSub { get; set; }
        public string FinishUninstallTitle { get; set; }
        public string FinishUninstallSub { get; set; }
        public string ErrorInstall { get; set; }
        public string ErrorUninstall { get; set; }
    }

    public class InstallerWindow : Window {
        private readonly bool _isUninstall;
        private string _targetDir;
        private string _currentLang = "pl";
        private InstallerStrings _s;
        private Dictionary<string, InstallerStrings> _languages;
        private bool _isCompleted = false;

        private TextBlock _titleTb;
        private TextBlock _headerTitle;
        private TextBlock _headerSubtitle;
        private TextBlock _langBtnText;
        private TextBox _pathBox;
        private TextBlock _pathLabel;
        private Button _browseBtn;
        private TextBlock _optLabel;
        private CheckBox _cbDesktop;
        private CheckBox _cbStartMenu;
        private CheckBox _cbAutostart;
        private CheckBox _cbLaunchNow;
        private TextBlock _uninstText;

        private Grid _mainGrid;
        private StackPanel _configPanel;
        private StackPanel _progressPanel;
        private StackPanel _finishPanel;

        private TextBlock _statusTitle;
        private TextBlock _statusSubtitle;
        private ProgressBar _progressBar;
        private Button _actionBtn;
        private TextBlock _finishTitle;
        private TextBlock _finishSub;

        public InstallerWindow(bool isUninstall) {
            _isUninstall = isUninstall;

            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _targetDir = Path.Combine(localApp, "Programs", "Rewind");

            InitLanguages();
            InitWindow();
            BuildUI();
        }

        private void InitLanguages() {
            _languages = new Dictionary<string, InstallerStrings>();

            // 1. Polski
            _languages["pl"] = new InstallerStrings {
                LangName = "Polski",
                Flag = "🇵🇱",
                TitleSetup = "Instalator Rewind v0.1",
                TitleUninstall = "Dezinstalacja Rewind",
                HeaderInstallTitle = "Instalacja programu Rewind",
                HeaderInstallSub = "Błyskawiczne zapisywanie powtórek z gier i pulpitu (NVIDIA NVENC).",
                HeaderUninstallTitle = "Odinstaluj program Rewind",
                HeaderUninstallSub = "Usunięcie programu, skrótów i wpisów systemowych.",
                PathLabel = "Katalog instalacji programu:",
                BrowseBtn = "Zmień...",
                BrowseDialogDesc = "Wybierz folder do instalacji programu Rewind:",
                OptLabel = "Opcje skrótów i autostartu:",
                CbDesktop = "Utwórz skrót na Pulpicie",
                CbStartMenu = "Dodaj do Menu Start (wyszukiwanie Windows)",
                CbAutostart = "Uruchamiaj z systemem Windows (w zasobniku)",
                CbLaunchNow = "Uruchom program Rewind po zakończeniu instalacji",
                UninstallConfirm = "Czy na pewno chcesz odinstalować program Rewind?\n\nWszystkie skróty z Pulpitu i Menu Start oraz wpisy autostartu zostaną usunięte z Twojego komputera. Twoje nagrane klipy wideo pozostaną nienaruszone.",
                ActionInstall = "Zainstaluj Rewind",
                ActionUninstall = "Odinstaluj program",
                ActionFinish = "Zakończ",
                ActionClose = "Zamknij",
                ProgressTitleInstall = "Instalowanie programu...",
                ProgressTitleUninstall = "Odinstalowywanie programu...",
                ProgressClosing = "Zamykanie aktywnych instancji programu...",
                ProgressDirectory = "Tworzenie katalogu docelowego...",
                ProgressCopying = "Kopiowanie plików aplikacji...",
                ProgressShortcuts = "Tworzenie skrótów systemowych...",
                ProgressRegistering = "Rejestracja w systemie Windows...",
                ProgressDoneInstall = "Instalacja ukończona pomyślnie!",
                ProgressClosingUninstall = "Zamykanie aktywnych procesów Rewind...",
                ProgressRemovingShortcuts = "Usuwanie skrótów systemowych...",
                ProgressRemovingRegistry = "Usuwanie wpisów z rejestru...",
                ProgressRemovingFiles = "Usuwanie plików aplikacji...",
                ProgressDoneUninstall = "Dezinstalacja zakończona!",
                FinishInstallTitle = "Rewind został pomyślnie zainstalowany!",
                FinishInstallSub = "Domyślny skrót do tworzenia klipów: [F8]. Program działa w tle.",
                FinishUninstallTitle = "Rewind został pomyślnie odinstalowany",
                FinishUninstallSub = "Wszystkie pliki i skróty programu zostały usunięte.",
                ErrorInstall = "Wystąpił błąd podczas instalacji:",
                ErrorUninstall = "Błąd podczas dezinstalacji:"
            };

            // 2. English
            _languages["en"] = new InstallerStrings {
                LangName = "English",
                Flag = "🇬🇧",
                TitleSetup = "Rewind Setup v0.1",
                TitleUninstall = "Uninstall Rewind",
                HeaderInstallTitle = "Install Rewind",
                HeaderInstallSub = "Instant replay buffer and game clipping engine (NVIDIA NVENC).",
                HeaderUninstallTitle = "Uninstall Rewind",
                HeaderUninstallSub = "Remove application, shortcuts, and registry entries.",
                PathLabel = "Installation directory:",
                BrowseBtn = "Browse...",
                BrowseDialogDesc = "Select destination folder for Rewind:",
                OptLabel = "Shortcuts & Startup Options:",
                CbDesktop = "Create Desktop shortcut",
                CbStartMenu = "Add to Start Menu (Windows Search)",
                CbAutostart = "Start with Windows (in system tray)",
                CbLaunchNow = "Launch Rewind when installation completes",
                UninstallConfirm = "Are you sure you want to uninstall Rewind?\n\nAll shortcuts and registry startup entries will be removed. Your saved video clips will remain untouched.",
                ActionInstall = "Install Rewind",
                ActionUninstall = "Uninstall Rewind",
                ActionFinish = "Finish",
                ActionClose = "Close",
                ProgressTitleInstall = "Installing Rewind...",
                ProgressTitleUninstall = "Uninstalling Rewind...",
                ProgressClosing = "Closing running instances...",
                ProgressDirectory = "Creating destination directory...",
                ProgressCopying = "Extracting application files...",
                ProgressShortcuts = "Creating system shortcuts...",
                ProgressRegistering = "Registering with Windows...",
                ProgressDoneInstall = "Installation completed successfully!",
                ProgressClosingUninstall = "Terminating running Rewind processes...",
                ProgressRemovingShortcuts = "Removing system shortcuts...",
                ProgressRemovingRegistry = "Cleaning registry entries...",
                ProgressRemovingFiles = "Deleting application files...",
                ProgressDoneUninstall = "Uninstallation complete!",
                FinishInstallTitle = "Rewind has been successfully installed!",
                FinishInstallSub = "Default clipping hotkey: [F8]. Running silently in background.",
                FinishUninstallTitle = "Rewind was successfully uninstalled",
                FinishUninstallSub = "All program files and shortcuts have been removed.",
                ErrorInstall = "An error occurred during installation:",
                ErrorUninstall = "An error occurred during uninstallation:"
            };

            // 3. Deutsch
            _languages["de"] = new InstallerStrings {
                LangName = "Deutsch",
                Flag = "🇩🇪",
                TitleSetup = "Rewind Setup v0.1",
                TitleUninstall = "Rewind deinstallieren",
                HeaderInstallTitle = "Rewind Installation",
                HeaderInstallSub = "Blitzschnelle Gaming-Wiederholungen und Clips (NVIDIA NVENC).",
                HeaderUninstallTitle = "Rewind deinstallieren",
                HeaderUninstallSub = "Entfernt Programm, Verknüpfungen und Registrierungseinträge.",
                PathLabel = "Installationsverzeichnis:",
                BrowseBtn = "Durchsuchen...",
                BrowseDialogDesc = "Zielordner für Rewind auswählen:",
                OptLabel = "Verknüpfungen und Autostart-Optionen:",
                CbDesktop = "Desktop-Verknüpfung erstellen",
                CbStartMenu = "Zum Startmenü hinzufügen (Windows-Suche)",
                CbAutostart = "Mit Windows starten (im Infobereich)",
                CbLaunchNow = "Rewind nach der Installation starten",
                UninstallConfirm = "Möchten Sie Rewind wirklich deinstallieren?\n\nAlle Verknüpfungen und Autostart-Einträge werden gelöscht. Ihre aufgenommenen Videoclips bleiben erhalten.",
                ActionInstall = "Rewind installieren",
                ActionUninstall = "Programm deinstallieren",
                ActionFinish = "Fertigstellen",
                ActionClose = "Schließen",
                ProgressTitleInstall = "Rewind wird installiert...",
                ProgressTitleUninstall = "Rewind wird deinstalliert...",
                ProgressClosing = "Laufende Instanzen werden beendet...",
                ProgressDirectory = "Zielordner wird erstellt...",
                ProgressCopying = "Programmdateien werden kopiert...",
                ProgressShortcuts = "Verknüpfungen werden erstellt...",
                ProgressRegistering = "In Windows registrieren...",
                ProgressDoneInstall = "Installation erfolgreich abgeschlossen!",
                ProgressClosingUninstall = "Prozesse werden beendet...",
                ProgressRemovingShortcuts = "Verknüpfungen werden entfernt...",
                ProgressRemovingRegistry = "Registrierungseinträge werden gelöscht...",
                ProgressRemovingFiles = "Dateien werden gelöscht...",
                ProgressDoneUninstall = "Deinstallation abgeschlossen!",
                FinishInstallTitle = "Rewind wurde erfolgreich installiert!",
                FinishInstallSub = "Standard-Tastenkombination: [F8]. Läuft leise im Hintergrund.",
                FinishUninstallTitle = "Rewind wurde deinstalliert",
                FinishUninstallSub = "Alle Dateien und Verknüpfungen wurden entfernt.",
                ErrorInstall = "Fehler bei der Installation:",
                ErrorUninstall = "Fehler bei der Deinstallation:"
            };

            // 4. Español
            _languages["es"] = new InstallerStrings {
                LangName = "Español",
                Flag = "🇪🇸",
                TitleSetup = "Instalador de Rewind v0.1",
                TitleUninstall = "Desinstalar Rewind",
                HeaderInstallTitle = "Instalar Rewind",
                HeaderInstallSub = "Captura instantánea de repeticiones en juegos y escritorio (NVIDIA NVENC).",
                HeaderUninstallTitle = "Desinstalar Rewind",
                HeaderUninstallSub = "Eliminar la aplicación, accesos directos y entradas del registro.",
                PathLabel = "Directorio de instalación:",
                BrowseBtn = "Examinar...",
                BrowseDialogDesc = "Seleccionar carpeta para Rewind:",
                OptLabel = "Opciones de accesos directos e inicio:",
                CbDesktop = "Crear acceso directo en el Escritorio",
                CbStartMenu = "Añadir al Menú Inicio (Búsqueda de Windows)",
                CbAutostart = "Iniciar con Windows (en la bandeja del sistema)",
                CbLaunchNow = "Iniciar Rewind al finalizar la instalación",
                UninstallConfirm = "¿Seguro que deseas desinstalar Rewind?\n\nSe eliminarán los accesos directos y entradas de inicio. Tus clips de video grabados no se modificarán.",
                ActionInstall = "Instalar Rewind",
                ActionUninstall = "Desinstalar",
                ActionFinish = "Finalizar",
                ActionClose = "Cerrar",
                ProgressTitleInstall = "Instalando Rewind...",
                ProgressTitleUninstall = "Desinstalando Rewind...",
                ProgressClosing = "Cerrando instancias activas...",
                ProgressDirectory = "Creando directorio de destino...",
                ProgressCopying = "Copiando archivos...",
                ProgressShortcuts = "Creando accesos directos...",
                ProgressRegistering = "Registrando en Windows...",
                ProgressDoneInstall = "¡Instalación completada con éxito!",
                ProgressClosingUninstall = "Cerrando procesos de Rewind...",
                ProgressRemovingShortcuts = "Eliminando accesos directos...",
                ProgressRemovingRegistry = "Limpiando registro...",
                ProgressRemovingFiles = "Eliminando archivos...",
                ProgressDoneUninstall = "¡Desinstalación completada!",
                FinishInstallTitle = "¡Rewind se ha instalado con éxito!",
                FinishInstallSub = "Atajo predeterminado para clips: [F8]. Funcionando en segundo plano.",
                FinishUninstallTitle = "Rewind se ha desinstalado",
                FinishUninstallSub = "Todos los archivos y accesos directos han sido eliminados.",
                ErrorInstall = "Ocurrió un error durante la instalación:",
                ErrorUninstall = "Ocurrió un error durante la desinstalación:"
            };

            // 5. Français
            _languages["fr"] = new InstallerStrings {
                LangName = "Français",
                Flag = "🇫🇷",
                TitleSetup = "Installation de Rewind v0.1",
                TitleUninstall = "Désinstaller Rewind",
                HeaderInstallTitle = "Installer Rewind",
                HeaderInstallSub = "Capture instantanée de clips et replay gaming (NVIDIA NVENC).",
                HeaderUninstallTitle = "Désinstaller Rewind",
                HeaderUninstallSub = "Suppression du programme, des raccourcis et du registre.",
                PathLabel = "Dossier d'installation :",
                BrowseBtn = "Parcourir...",
                BrowseDialogDesc = "Choisir le dossier de destination pour Rewind :",
                OptLabel = "Options de raccourcis et démarrage :",
                CbDesktop = "Créer un raccourci sur le Bureau",
                CbStartMenu = "Ajouter au Menu Démarrer (Recherche Windows)",
                CbAutostart = "Lancer avec Windows (dans la barre des tâches)",
                CbLaunchNow = "Lancer Rewind à la fin de l'installation",
                UninstallConfirm = "Voulez-vous vraiment désinstaller Rewind ?\n\nTous les raccourcis et entrées de démarrage seront supprimés. Vos vidéos enregistrées resteront intactes.",
                ActionInstall = "Installer Rewind",
                ActionUninstall = "Désinstaller",
                ActionFinish = "Terminer",
                ActionClose = "Fermer",
                ProgressTitleInstall = "Installation de Rewind...",
                ProgressTitleUninstall = "Désinstallation de Rewind...",
                ProgressClosing = "Fermeture des instances en cours...",
                ProgressDirectory = "Création du dossier cible...",
                ProgressCopying = "Copie des fichiers de l'application...",
                ProgressShortcuts = "Création des raccourcis...",
                ProgressRegistering = "Enregistrement dans Windows...",
                ProgressDoneInstall = "Installation terminée avec succès !",
                ProgressClosingUninstall = "Arrêt des processus Rewind...",
                ProgressRemovingShortcuts = "Suppression des raccourcis...",
                ProgressRemovingRegistry = "Nettoyage du registre...",
                ProgressRemovingFiles = "Suppression des fichiers...",
                ProgressDoneUninstall = "Désinstallation terminée !",
                FinishInstallTitle = "Rewind a été installé avec succès !",
                FinishInstallSub = "Raccourci par défaut : [F8]. Fonctionne discrètement en arrière-plan.",
                FinishUninstallTitle = "Rewind a été désinstallé",
                FinishUninstallSub = "Tous les fichiers et raccourcis ont été supprimés.",
                ErrorInstall = "Une erreur est survenue lors de l'installation :",
                ErrorUninstall = "Une erreur est survenue lors de la désinstallation :"
            };

            // 6. Українська
            _languages["ua"] = new InstallerStrings {
                LangName = "Українська",
                Flag = "🇺🇦",
                TitleSetup = "Встановлення Rewind v0.1",
                TitleUninstall = "Видалення Rewind",
                HeaderInstallTitle = "Встановлення Rewind",
                HeaderInstallSub = "Миттєвий запис ігрових повторів та кліпів (NVIDIA NVENC).",
                HeaderUninstallTitle = "Видалити Rewind",
                HeaderUninstallSub = "Видалення програми, ярликів та записів реєстру.",
                PathLabel = "Папка для встановлення:",
                BrowseBtn = "Огляд...",
                BrowseDialogDesc = "Оберіть папку для встановлення Rewind:",
                OptLabel = "Параметри ярликів та автозапуску:",
                CbDesktop = "Створити ярлик на Робочому столі",
                CbStartMenu = "Додати до меню «Пуск» (пошук Windows)",
                CbAutostart = "Запускати разом з Windows (у треї)",
                CbLaunchNow = "Запустити Rewind після завершення встановлення",
                UninstallConfirm = "Ви дійсно бажаєте видалити Rewind?\n\nВсі ярлики та записи автозапуску будуть видалені з комп'ютера. Ваші записані відеокліпи залишаться недоторканими.",
                ActionInstall = "Встановити Rewind",
                ActionUninstall = "Видалити програму",
                ActionFinish = "Завершити",
                ActionClose = "Закрити",
                ProgressTitleInstall = "Встановлення програми...",
                ProgressTitleUninstall = "Видалення програми...",
                ProgressClosing = "Закриття активних процесів Rewind...",
                ProgressDirectory = "Створення цільової папки...",
                ProgressCopying = "Копіювання файлів...",
                ProgressShortcuts = "Створення ярликів...",
                ProgressRegistering = "Реєстрація в системі Windows...",
                ProgressDoneInstall = "Встановлення успішно завершено!",
                ProgressClosingUninstall = "Завершення процесів Rewind...",
                ProgressRemovingShortcuts = "Видалення ярликів...",
                ProgressRemovingRegistry = "Очищення реєстру...",
                ProgressRemovingFiles = "Видалення файлів...",
                ProgressDoneUninstall = "Видалення завершено!",
                FinishInstallTitle = "Rewind успішно встановлено!",
                FinishInstallSub = "Гаряча клавіша для кліпу: [F8]. Програма працює у фоні.",
                FinishUninstallTitle = "Rewind успішно видалено",
                FinishUninstallSub = "Всі файли та ярлики програми видалено.",
                ErrorInstall = "Виникла помилка під час встановлення:",
                ErrorUninstall = "Виникла помилка під час видалення:"
            };

            // Detect system language
            string uiLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (uiLang == "pl") _currentLang = "pl";
            else if (uiLang == "de") _currentLang = "de";
            else if (uiLang == "es") _currentLang = "es";
            else if (uiLang == "fr") _currentLang = "fr";
            else if (uiLang == "uk") _currentLang = "ua";
            else if (uiLang == "en") _currentLang = "en";
            else _currentLang = "pl";

            _s = _languages[_currentLang];
        }

        private void SetLanguage(string code) {
            if (!_languages.ContainsKey(code)) return;
            _currentLang = code;
            _s = _languages[code];

            Title = _isUninstall ? _s.TitleUninstall : _s.TitleSetup;
            if (_titleTb != null) _titleTb.Text = _isUninstall ? _s.TitleUninstall : _s.TitleSetup;
            if (_headerTitle != null) _headerTitle.Text = _isUninstall ? _s.HeaderUninstallTitle : _s.HeaderInstallTitle;
            if (_headerSubtitle != null) _headerSubtitle.Text = _isUninstall ? _s.HeaderUninstallSub : _s.HeaderInstallSub;
            if (_langBtnText != null) _langBtnText.Text = _s.Flag + " " + _s.LangName + " ▾";

            if (_pathLabel != null) _pathLabel.Text = _s.PathLabel;
            if (_browseBtn != null) _browseBtn.Content = _s.BrowseBtn;
            if (_optLabel != null) _optLabel.Text = _s.OptLabel;
            if (_cbDesktop != null) _cbDesktop.Content = _s.CbDesktop;
            if (_cbStartMenu != null) _cbStartMenu.Content = _s.CbStartMenu;
            if (_cbAutostart != null) _cbAutostart.Content = _s.CbAutostart;
            if (_cbLaunchNow != null) _cbLaunchNow.Content = _s.CbLaunchNow;
            if (_uninstText != null) _uninstText.Text = _s.UninstallConfirm;

            if (_statusTitle != null) _statusTitle.Text = _isUninstall ? _s.ProgressTitleUninstall : _s.ProgressTitleInstall;
            if (_finishTitle != null) _finishTitle.Text = _isUninstall ? _s.FinishUninstallTitle : _s.FinishInstallTitle;
            if (_finishSub != null) _finishSub.Text = _isUninstall ? _s.FinishUninstallSub : _s.FinishInstallSub;

            if (_actionBtn != null) {
                if (_isCompleted) {
                    _actionBtn.Content = _isUninstall ? _s.ActionClose : _s.ActionFinish;
                } else {
                    _actionBtn.Content = _isUninstall ? _s.ActionUninstall : _s.ActionInstall;
                }
            }
        }

        private void InitWindow() {
            Title = _isUninstall ? _s.TitleUninstall : _s.TitleSetup;
            Width = 560;
            Height = 460;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)); // Pitch Black
            Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI Emoji, Segoe UI, sans-serif");
            UseLayoutRounding = true;

            var chrome = new WindowChrome {
                CaptionHeight = 44,
                CornerRadius = new CornerRadius(14),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            var iconImg = LoadAppIconImage();
            if (iconImg != null) {
                Icon = iconImg;
            }
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
                        var decoder = new System.Windows.Media.Imaging.IconBitmapDecoder(ms, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                        System.Windows.Media.Imaging.BitmapFrame best = null;
                        foreach (var f in decoder.Frames) {
                            if (best == null || f.PixelWidth > best.PixelWidth) best = f;
                        }
                        return best ?? (decoder.Frames.Count > 0 ? decoder.Frames[0] : null);
                    }
                }
            } catch { }
            return null;
        }

        private void BuildUI() {
            var outerBorder = new Border {
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10))
            };
            Content = outerBorder;

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Title bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) }); // Brand header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) }); // Footer action
            outerBorder.Child = rootGrid;

            // 1. Title bar
            var titleBar = new Grid();
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _titleTb = new TextBlock {
                Text = _isUninstall ? _s.TitleUninstall : _s.TitleSetup,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 0, 0)
            };
            Grid.SetColumn(_titleTb, 0);
            titleBar.Children.Add(_titleTb);

            var titleRightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Language Selector Button
            var langSelector = CreateLanguageSelector();
            titleRightStack.Children.Add(langSelector);

            var closeBtn = new Button {
                Width = 44,
                Height = 44,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Content = new TextBlock {
                    Text = "✕",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            StyleModernButton(closeBtn, 0);
            WindowChrome.SetIsHitTestVisibleInChrome(closeBtn, true);
            closeBtn.Click += delegate { Close(); };
            titleRightStack.Children.Add(closeBtn);

            Grid.SetColumn(titleRightStack, 1);
            titleBar.Children.Add(titleRightStack);

            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            // 2. Brand Header
            var headerStack = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 0, 24, 0)
            };

            var logoBox = new Border {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                Background = new LinearGradientBrush(
                    Color.FromRgb(139, 92, 246), // Electric Violet Accent
                    Color.FromRgb(99, 102, 241),
                    45.0
                ),
                Margin = new Thickness(0, 0, 14, 0),
                Child = new TextBlock {
                    Text = "✦",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            headerStack.Children.Add(logoBox);

            var headerTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _headerTitle = new TextBlock {
                Text = _isUninstall ? _s.HeaderUninstallTitle : _s.HeaderInstallTitle,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White
            };
            headerTextStack.Children.Add(_headerTitle);

            _headerSubtitle = new TextBlock {
                Text = _isUninstall ? _s.HeaderUninstallSub : _s.HeaderInstallSub,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            headerTextStack.Children.Add(_headerSubtitle);
            headerStack.Children.Add(headerTextStack);

            Grid.SetRow(headerStack, 1);
            rootGrid.Children.Add(headerStack);

            // 3. Main Center Area
            _mainGrid = new Grid { Margin = new Thickness(24, 6, 24, 6) };
            Grid.SetRow(_mainGrid, 2);
            rootGrid.Children.Add(_mainGrid);

            BuildViews();

            // 4. Footer Action
            var footerBorder = new Border {
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 14)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 0, 24, 0)
            };
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var hintTb = new TextBlock {
                Text = "Rewind v0.1 Standalone",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(hintTb, 0);
            footerGrid.Children.Add(hintTb);

            _actionBtn = new Button {
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new LinearGradientBrush(
                    Color.FromRgb(139, 92, 246), // Electric Violet
                    Color.FromRgb(109, 40, 217),
                    45.0
                ),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Padding = new Thickness(24, 10, 24, 10),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                Content = _isUninstall ? _s.ActionUninstall : _s.ActionInstall
            };
            StyleModernButton(_actionBtn, 12);
            _actionBtn.Click += delegate {
                if (_isUninstall) {
                    RunUninstallation();
                } else {
                    RunInstallation();
                }
            };
            Grid.SetColumn(_actionBtn, 1);
            footerGrid.Children.Add(_actionBtn);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 3);
            rootGrid.Children.Add(footerBorder);
        }

        private FrameworkElement CreateLanguageSelector() {
            var grid = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };

            var langBtn = new Button {
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            StyleModernButton(langBtn, 7);
            WindowChrome.SetIsHitTestVisibleInChrome(langBtn, true);

            _langBtnText = new TextBlock {
                Text = _s.Flag + " " + _s.LangName + " ▾",
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(215, 215, 215)),
                VerticalAlignment = VerticalAlignment.Center
            };
            langBtn.Content = _langBtnText;

            var popup = new Popup {
                PlacementTarget = langBtn,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            var popupBorder = new Border {
                Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                MinWidth = 145
            };

            var itemsStack = new StackPanel { Orientation = Orientation.Vertical };

            Action refreshItems = delegate {
                itemsStack.Children.Clear();
                foreach (var kvp in _languages) {
                    string code = kvp.Key;
                    var langData = kvp.Value;
                    bool isSelected = (code == _currentLang);

                    var itemBtn = new Button {
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Height = 28,
                        Background = isSelected ? new SolidColorBrush(Color.FromArgb(40, 139, 92, 246)) : System.Windows.Media.Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 0, 8, 0),
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    StyleModernButton(itemBtn, 6);

                    var itemText = new TextBlock {
                        Text = langData.Flag + "  " + langData.LangName,
                        FontSize = 11.5,
                        Foreground = isSelected ? new SolidColorBrush(Color.FromRgb(196, 181, 253)) : new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    itemBtn.Content = itemText;

                    itemBtn.MouseEnter += delegate {
                        itemBtn.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
                        itemText.Foreground = System.Windows.Media.Brushes.White;
                    };
                    itemBtn.MouseLeave += delegate {
                        itemBtn.Background = (code == _currentLang) ? new SolidColorBrush(Color.FromArgb(40, 139, 92, 246)) : System.Windows.Media.Brushes.Transparent;
                        itemText.Foreground = (code == _currentLang) ? new SolidColorBrush(Color.FromRgb(196, 181, 253)) : new SolidColorBrush(Color.FromRgb(220, 220, 220));
                    };

                    itemBtn.Click += delegate {
                        popup.IsOpen = false;
                        SetLanguage(code);
                    };

                    itemsStack.Children.Add(itemBtn);
                }
            };

            langBtn.Click += delegate {
                refreshItems();
                popup.IsOpen = !popup.IsOpen;
            };

            popupBorder.Child = itemsStack;
            popup.Child = popupBorder;

            grid.Children.Add(langBtn);
            grid.Children.Add(popup);
            return grid;
        }

        private void BuildViews() {
            // View 1: Config
            _configPanel = new StackPanel { Orientation = Orientation.Vertical };

            var card = new Border {
                Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var cardStack = new StackPanel { Orientation = Orientation.Vertical };

            if (!_isUninstall) {
                // Folder path row
                var pathRow = new Grid();
                pathRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                pathRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                _pathLabel = new TextBlock {
                    Text = _s.PathLabel,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                Grid.SetRow(_pathLabel, 0);
                pathRow.Children.Add(_pathLabel);

                var inputGrid = new Grid();
                inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                _pathBox = new TextBox {
                    Text = _targetDir,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(46, 46, 46)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 6, 8, 6),
                    IsReadOnly = true,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(_pathBox, 0);
                inputGrid.Children.Add(_pathBox);

                _browseBtn = new Button {
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                    BorderThickness = new Thickness(1),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 11.5,
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(8, 0, 0, 0),
                    Content = _s.BrowseBtn
                };
                StyleModernButton(_browseBtn, 8);
                _browseBtn.Click += delegate {
                    var fbd = new FolderBrowserDialog {
                        Description = _s.BrowseDialogDesc,
                        SelectedPath = _targetDir
                    };
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                        _targetDir = Path.Combine(fbd.SelectedPath, "Rewind");
                        _pathBox.Text = _targetDir;
                    }
                };
                Grid.SetColumn(_browseBtn, 1);
                inputGrid.Children.Add(_browseBtn);

                Grid.SetRow(inputGrid, 1);
                pathRow.Children.Add(inputGrid);
                cardStack.Children.Add(pathRow);

                // Divider
                cardStack.Children.Add(new Border {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                    Margin = new Thickness(0, 12, 0, 12)
                });

                // Options
                _optLabel = new TextBlock {
                    Text = _s.OptLabel,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                cardStack.Children.Add(_optLabel);

                _cbDesktop = CreateStyledCheckBox(_s.CbDesktop, true);
                cardStack.Children.Add(_cbDesktop);

                _cbStartMenu = CreateStyledCheckBox(_s.CbStartMenu, true);
                cardStack.Children.Add(_cbStartMenu);

                _cbAutostart = CreateStyledCheckBox(_s.CbAutostart, true);
                cardStack.Children.Add(_cbAutostart);

                _cbLaunchNow = CreateStyledCheckBox(_s.CbLaunchNow, true);
                cardStack.Children.Add(_cbLaunchNow);
            } else {
                _uninstText = new TextBlock {
                    Text = _s.UninstallConfirm,
                    FontSize = 12.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                };
                cardStack.Children.Add(_uninstText);
            }

            card.Child = cardStack;
            _configPanel.Children.Add(card);
            _mainGrid.Children.Add(_configPanel);

            // View 2: Progress
            _progressPanel = new StackPanel {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            _statusTitle = new TextBlock {
                Text = _isUninstall ? _s.ProgressTitleUninstall : _s.ProgressTitleInstall,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _progressPanel.Children.Add(_statusTitle);

            _statusSubtitle = new TextBlock {
                Text = _s.ProgressCopying,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18)
            };
            _progressPanel.Children.Add(_statusSubtitle);

            _progressBar = new ProgressBar {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(40, 0, 40, 0)
            };
            _progressPanel.Children.Add(_progressBar);
            _mainGrid.Children.Add(_progressPanel);

            // View 3: Finish
            _finishPanel = new StackPanel {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            var checkBadge = new Border {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(23),
                Background = new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12),
                Child = new TextBlock {
                    Text = "✓",
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            _finishPanel.Children.Add(checkBadge);

            _finishTitle = new TextBlock {
                Text = _isUninstall ? _s.FinishUninstallTitle : _s.FinishInstallTitle,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _finishPanel.Children.Add(_finishTitle);

            _finishSub = new TextBlock {
                Text = _isUninstall ? _s.FinishUninstallSub : _s.FinishInstallSub,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _finishPanel.Children.Add(_finishSub);
            _mainGrid.Children.Add(_finishPanel);
        }

        private CheckBox CreateStyledCheckBox(string text, bool isChecked) {
            var cb = new CheckBox {
                Content = text,
                IsChecked = isChecked,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Margin = new Thickness(0, 3, 0, 5),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            return cb;
        }

        private void RunInstallation() {
            _actionBtn.IsEnabled = false;
            _configPanel.Visibility = Visibility.Collapsed;
            _progressPanel.Visibility = Visibility.Visible;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    UpdateProgress(15, _s.ProgressClosing);
                    foreach (var p in Process.GetProcessesByName("Rewind")) {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }

                    UpdateProgress(30, _s.ProgressDirectory);
                    if (!Directory.Exists(_targetDir)) {
                        Directory.CreateDirectory(_targetDir);
                    }

                    string destExe = Path.Combine(_targetDir, "Rewind.exe");
                    string destUninstaller = Path.Combine(_targetDir, "Uninstall.exe");

                    UpdateProgress(50, _s.ProgressCopying);
                    // Extract payload Rewind.exe
                    var asm = Assembly.GetExecutingAssembly();
                    using (var resStream = asm.GetManifestResourceStream("rewind_payload.exe")) {
                        if (resStream != null) {
                            using (var fs = new FileStream(destExe, FileMode.Create, FileAccess.Write)) {
                                byte[] buf = new byte[65536];
                                int read;
                                while ((read = resStream.Read(buf, 0, buf.Length)) > 0) {
                                    fs.Write(buf, 0, read);
                                }
                            }
                        } else {
                            // Fallback if built alongside
                            string localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rewind.exe");
                            if (File.Exists(localExe)) {
                                File.Copy(localExe, destExe, true);
                            }
                        }
                    }

                    // Copy self as Uninstall.exe
                    string selfPath = Process.GetCurrentProcess().MainModule.FileName;
                    if (File.Exists(selfPath)) {
                        try { File.Copy(selfPath, destUninstaller, true); } catch { }
                    }

                    UpdateProgress(75, _s.ProgressShortcuts);
                    bool createDesktop = false;
                    bool createStartMenu = false;
                    bool autostart = false;
                    bool launchNow = false;

                    Dispatcher.Invoke(new Action(delegate {
                        createDesktop = _cbDesktop.IsChecked == true;
                        createStartMenu = _cbStartMenu.IsChecked == true;
                        autostart = _cbAutostart.IsChecked == true;
                        launchNow = _cbLaunchNow.IsChecked == true;
                    }));

                    if (createDesktop) {
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string deskLnk = Path.Combine(desktopPath, "Rewind.lnk");
                        CreateShortcut(deskLnk, destExe, "Rewind - Replay Buffer & Clipping");
                        SetAppUserModelId(deskLnk, "Rewind.App");
                    }

                    if (createStartMenu) {
                        string programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                        if (!Directory.Exists(programsPath)) {
                            Directory.CreateDirectory(programsPath);
                        }

                        // 1. Root of Programs (for instant Windows Search indexing)
                        string rootLnk = Path.Combine(programsPath, "Rewind.lnk");
                        CreateShortcut(rootLnk, destExe, "Rewind - Replay Buffer & Clipping");
                        SetAppUserModelId(rootLnk, "Rewind.App");

                        // 2. Programs\Rewind folder
                        string subFolder = Path.Combine(programsPath, "Rewind");
                        if (!Directory.Exists(subFolder)) {
                            Directory.CreateDirectory(subFolder);
                        }
                        string subLnk = Path.Combine(subFolder, "Rewind.lnk");
                        CreateShortcut(subLnk, destExe, "Rewind - Replay Buffer & Clipping");
                        SetAppUserModelId(subLnk, "Rewind.App");
                    }

                    UpdateProgress(90, _s.ProgressRegistering);
                    RegisterUninstall(destUninstaller, destExe);
                    RegisterAppPaths(destExe);

                    if (autostart) {
                        SetAutostart(destExe, true);
                    }

                    // Shell notification so Windows Search and Explorer index the new app immediately
                    NotifyShell();

                    UpdateProgress(100, _s.ProgressDoneInstall);
                    Thread.Sleep(400);

                    if (launchNow) {
                        try {
                            Process.Start(new ProcessStartInfo(destExe) { WorkingDirectory = _targetDir });
                        } catch { }
                    }

                    Dispatcher.Invoke(new Action(delegate {
                        _isCompleted = true;
                        _progressPanel.Visibility = Visibility.Collapsed;
                        _finishPanel.Visibility = Visibility.Visible;
                        _actionBtn.IsEnabled = true;
                        _actionBtn.Content = _s.ActionFinish;
                        _actionBtn.Click += delegate { Close(); };
                    }));
                } catch (Exception ex) {
                    Dispatcher.Invoke(new Action(delegate {
                        MessageBox.Show(_s.ErrorInstall + "\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        _progressPanel.Visibility = Visibility.Collapsed;
                        _configPanel.Visibility = Visibility.Visible;
                        _actionBtn.IsEnabled = true;
                    }));
                }
            });
        }

        private void RunUninstallation() {
            _actionBtn.IsEnabled = false;
            _configPanel.Visibility = Visibility.Collapsed;
            _progressPanel.Visibility = Visibility.Visible;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    UpdateProgress(20, _s.ProgressClosingUninstall);
                    foreach (var p in Process.GetProcessesByName("Rewind")) {
                        try { p.Kill(); p.WaitForExit(1500); } catch { }
                    }

                    UpdateProgress(40, _s.ProgressRemovingShortcuts);
                    string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Rewind.lnk");
                    try { if (File.Exists(desktopLink)) File.Delete(desktopLink); } catch { }

                    string programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string startMenuLink = Path.Combine(programsPath, "Rewind.lnk");
                    try { if (File.Exists(startMenuLink)) File.Delete(startMenuLink); } catch { }

                    string subDir = Path.Combine(programsPath, "Rewind");
                    try { if (Directory.Exists(subDir)) Directory.Delete(subDir, true); } catch { }

                    UpdateProgress(60, _s.ProgressRemovingRegistry);
                    UnregisterUninstall();
                    UnregisterAppPaths();
                    SetAutostart(null, false);
                    NotifyShell();

                    UpdateProgress(80, _s.ProgressRemovingFiles);
                    string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                    string appDir = Path.GetDirectoryName(currentExe);

                    string exePath = Path.Combine(appDir, "Rewind.exe");
                    try { if (File.Exists(exePath)) File.Delete(exePath); } catch { }

                    UpdateProgress(100, _s.ProgressDoneUninstall);
                    Thread.Sleep(300);

                    Dispatcher.Invoke(new Action(delegate {
                        _isCompleted = true;
                        _progressPanel.Visibility = Visibility.Collapsed;
                        _finishPanel.Visibility = Visibility.Visible;
                        _actionBtn.IsEnabled = true;
                        _actionBtn.Content = _s.ActionClose;
                        _actionBtn.Click += delegate { Close(); };
                    }));
                } catch (Exception ex) {
                    Dispatcher.Invoke(new Action(delegate {
                        MessageBox.Show(_s.ErrorUninstall + "\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    }));
                }
            });
        }

        private void UpdateProgress(int percent, string text) {
            Dispatcher.Invoke(new Action(delegate {
                _progressBar.Value = percent;
                _statusSubtitle.Text = text;
            }));
        }

        private static void CreateShortcut(string shortcutPath, string targetExePath, string description) {
            try {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
                shortcut.Description = description;
                shortcut.IconLocation = targetExePath + ",0";
                shortcut.Save();
            } catch { }
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
        private class ShellLinkClass {}

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PROPERTYKEY pkey);
            void GetValue(ref PROPERTYKEY key, [Out] PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, [In] PROPVARIANT pv);
            void Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY {
            public Guid fmtid;
            public uint pid;
            public PROPERTYKEY(Guid g, uint p) { fmtid = g; pid = p; }
        }

        [StructLayout(LayoutKind.Explicit)]
        private class PROPVARIANT : IDisposable {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;

            public PROPVARIANT(string s) {
                vt = 31; // VT_LPWSTR
                pwszVal = Marshal.StringToCoTaskMemUni(s);
            }
            public void Dispose() {
                if (pwszVal != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pwszVal);
                    pwszVal = IntPtr.Zero;
                }
            }
        }

        private static readonly PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

        private static void SetAppUserModelId(string lnkPath, string appId) {
            try {
                var shellLink = new ShellLinkClass();
                var persistFile = (System.Runtime.InteropServices.ComTypes.IPersistFile)shellLink;
                persistFile.Load(lnkPath, 2); // STGM_READWRITE = 2
                var store = (IPropertyStore)shellLink;
                PROPERTYKEY key = PKEY_AppUserModel_ID;
                using (var pv = new PROPVARIANT(appId)) {
                    store.SetValue(ref key, pv);
                    store.Commit();
                }
                persistFile.Save(lnkPath, true);
            } catch { }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNE_UPDATEDIR = 0x00001000;

        private static void NotifyShell() {
            try {
                SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                IntPtr pPath = Marshal.StringToCoTaskMemUni(programs);
                SHChangeNotify(SHCNE_UPDATEDIR, 0x0005 /* SHCNF_PATHW */, pPath, IntPtr.Zero);
                Marshal.FreeCoTaskMem(pPath);
            } catch { }
        }

        private static void RegisterAppPaths(string mainExePath) {
            try {
                // 1. HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\Rewind.exe
                string appPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\Rewind.exe";
                using (var key = Registry.CurrentUser.CreateSubKey(appPathsKey)) {
                    if (key != null) {
                        key.SetValue("", mainExePath);
                        key.SetValue("Path", Path.GetDirectoryName(mainExePath));
                    }
                }

                // 2. HKCU\Software\Classes\Applications\Rewind.exe
                string appClassesKey = @"Software\Classes\Applications\Rewind.exe";
                using (var key = Registry.CurrentUser.CreateSubKey(appClassesKey)) {
                    if (key != null) {
                        key.SetValue("FriendlyAppName", "Rewind");
                        using (var cmd = key.CreateSubKey(@"shell\open\command")) {
                            if (cmd != null) {
                                cmd.SetValue("", string.Format("\"{0}\" \"%1\"", mainExePath));
                            }
                        }
                    }
                }

                // 3. HKCU\Software\Classes\Rewind.App
                string progIdKey = @"Software\Classes\Rewind.App";
                using (var key = Registry.CurrentUser.CreateSubKey(progIdKey)) {
                    if (key != null) {
                        key.SetValue("", "Rewind");
                        key.SetValue("FriendlyTypeName", "Rewind Replay Buffer");
                        using (var icon = key.CreateSubKey("DefaultIcon")) {
                            if (icon != null) icon.SetValue("", mainExePath + ",0");
                        }
                        using (var cmd = key.CreateSubKey(@"shell\open\command")) {
                            if (cmd != null) cmd.SetValue("", string.Format("\"{0}\"", mainExePath));
                        }
                    }
                }
            } catch { }
        }

        private static void UnregisterAppPaths() {
            try {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\App Paths\Rewind.exe", false);
            } catch { }
            try {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Applications\Rewind.exe", false);
            } catch { }
            try {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Rewind.App", false);
            } catch { }
        }

        private static void RegisterUninstall(string uninstallerPath, string mainExePath) {
            try {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Rewind";
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath)) {
                    if (key != null) {
                        key.SetValue("DisplayName", "Rewind");
                        key.SetValue("DisplayVersion", "0.1");
                        key.SetValue("Publisher", "Rewind");
                        key.SetValue("DisplayIcon", mainExePath + ",0");
                        key.SetValue("InstallLocation", Path.GetDirectoryName(mainExePath));
                        key.SetValue("UninstallString", string.Format("\"{0}\" --uninstall", uninstallerPath));
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            } catch { }
        }

        private static void UnregisterUninstall() {
            try {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Rewind";
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
            } catch { }
        }

        private static void SetAutostart(string exePath, bool enable) {
            try {
                string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using (var key = Registry.CurrentUser.OpenSubKey(runKey, true)) {
                    if (key != null) {
                        if (enable && !string.IsNullOrEmpty(exePath)) {
                            key.SetValue("Rewind", string.Format("\"{0}\" --minimized", exePath));
                        } else {
                            try { key.DeleteValue("Rewind", false); } catch { }
                        }
                    }
                }
            } catch { }
        }
    }
}
