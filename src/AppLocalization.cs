using System;
using System.Collections.Generic;

namespace ClipFlow {
    public class UiStrings {
        // Navigation
        public string NavClips { get; set; }
        public string NavVideo { get; set; }
        public string NavAudio { get; set; }
        public string NavHotkeys { get; set; }
        public string NavSystem { get; set; }
        public string NavAbout { get; set; }

        // Hero Card
        public string HeroHotkeyPrefix { get; set; }
        public string HeroTitleFormat { get; set; }
        public string HeroSubtitle { get; set; }
        public string HeroButton { get; set; }

        // Sidebar Footer
        public string SidebarStatusActive { get; set; }
        public string SidebarStatusPaused { get; set; }
        public string SidebarInfoFormat { get; set; }

        // Clips View
        public string ClipsTitle { get; set; }
        public string ClipsOpenFolder { get; set; }
        public string ClipsEmpty { get; set; }
        public string ClipsPlay { get; set; }
        public string ClipsDelete { get; set; }
        public string ClipsCopyPath { get; set; }

        // Video Settings
        public string VideoScreenTitle { get; set; }
        public string VideoScreenSub { get; set; }
        public string ScreenLabel { get; set; }
        public string ScreenPrimaryBadge { get; set; }
        public string ScreenPrimaryDefault { get; set; }
        public string VideoBufferTitle { get; set; }
        public string VideoBufferSub { get; set; }
        public string VideoDurationTitle { get; set; }
        public string VideoDurationSub { get; set; }
        public string VideoFpsTitle { get; set; }
        public string VideoFpsSub { get; set; }
        public string VideoBitrateTitle { get; set; }
        public string VideoBitrateSub { get; set; }
        public string VideoCursorTitle { get; set; }
        public string VideoCursorSub { get; set; }
        public string VideoFolderTitle { get; set; }
        public string VideoFolderSub { get; set; }
        public string VideoFolderBtn { get; set; }

        // Audio Settings
        public string AudioPlaySoundTitle { get; set; }
        public string AudioPlaySoundSub { get; set; }
        public string AudioPresetTitle { get; set; }
        public string AudioPresetSub { get; set; }
        public string AudioTestBtn { get; set; }
        public string AudioCustomTitle { get; set; }
        public string AudioCustomSub { get; set; }
        public string AudioCustomBtn { get; set; }
        public string AudioSysVolTitle { get; set; }
        public string AudioSysVolSub { get; set; }
        public string AudioMicVolTitle { get; set; }
        public string AudioMicVolSub { get; set; }
        public string AudioMicDeviceTitle { get; set; }
        public string AudioMicDeviceSub { get; set; }

        // Hotkeys Settings
        public string HotkeyTitle { get; set; }
        public string HotkeySub { get; set; }

        // System Settings
        public string SystemLangTitle { get; set; }
        public string SystemLangSub { get; set; }
        public string SystemAutostartTitle { get; set; }
        public string SystemAutostartSub { get; set; }
        public string SystemStartMinTitle { get; set; }
        public string SystemStartMinSub { get; set; }
        public string SystemHudTitle { get; set; }
        public string SystemHudSub { get; set; }
        public string SystemThemeTitle { get; set; }
        public string SystemThemeSub { get; set; }

        // About Settings
        public string AboutVersionTitle { get; set; }
        public string AboutVersionSub { get; set; }
        public string AboutGpuTitle { get; set; }
        public string AboutGpuSub { get; set; }
        public string AboutAudioTitle { get; set; }
        public string AboutAudioSub { get; set; }
        public string AboutFootprintTitle { get; set; }
        public string AboutFootprintSub { get; set; }

        // Tray Menu
        public string TrayHeader { get; set; }
        public string TrayBufferActive { get; set; }
        public string TrayBufferPaused { get; set; }
        public string TraySaveClip { get; set; }
        public string TrayOpenFolder { get; set; }
        public string TrayPauseBuffer { get; set; }
        public string TrayResumeBuffer { get; set; }
        public string TraySettings { get; set; }
        public string TrayExit { get; set; }

        // Notifications
        public string HudTitleFormat { get; set; }
        public string HudSubtitle { get; set; }
        public string HudOpenBtn { get; set; }
        public string BalloonTitle { get; set; }
        public string BalloonSub { get; set; }

        // Additional UI & Details
        public string ClipsRevealBtn { get; set; }
        public string ClipsDeleteConfirm { get; set; }
        public string ClipsDeleteTitle { get; set; }

        public string SectionHeaderVideo { get; set; }
        public string SectionHeaderVideoSub { get; set; }
        public string SectionHeaderAudio { get; set; }
        public string SectionHeaderAudioSub { get; set; }
        public string SectionHeaderHotkeys { get; set; }
        public string SectionHeaderHotkeysSub { get; set; }
        public string SectionHeaderSystem { get; set; }
        public string SectionHeaderSystemSub { get; set; }

        public string VideoGpuTitle { get; set; }
        public string VideoGpuSub { get; set; }
        public string HotkeyGlobalTitle { get; set; }
        public string HotkeyGlobalSub { get; set; }
        public string HotkeyBadgeText { get; set; }
        public string SystemBufferActivityTitle { get; set; }
        public string SystemBufferActivitySub { get; set; }

        public string AudioMicDefault { get; set; }
        public string SoundPresetDefault { get; set; }
        public string SoundPresetShutter { get; set; }
        public string SoundPresetDing { get; set; }
        public string SoundPresetRetro { get; set; }
        public string SoundPresetCustom { get; set; }
        public string SoundCustomEmpty { get; set; }

        public string ToastClipSaved { get; set; }
        public string ToastPlayNow { get; set; }
        public string TooltipLightMode { get; set; }
        public string TooltipDarkMode { get; set; }
        public string TooltipMinimize { get; set; }
        public string TooltipClose { get; set; }
        public string FooterEscHint { get; set; }
    }

    public static class AppLocalization {
        private static readonly Dictionary<string, UiStrings> _dict = new Dictionary<string, UiStrings>(StringComparer.OrdinalIgnoreCase);

        static AppLocalization() {
            // 1. Polski
            _dict["pl"] = new UiStrings {
                NavClips = "🎬  Moje powtórki",
                NavVideo = "📹  Wideo i bufor",
                NavAudio = "🎧  Dźwięk i mikser",
                NavHotkeys = "⌨️  Skróty klawiszowe",
                NavSystem = "⚙️  System i autostart",
                NavAbout = "ℹ️  O programie",

                HeroHotkeyPrefix = "SKRÓT: ",
                HeroTitleFormat = "Zapisz ostatnie {0} sekund",
                HeroSubtitle = "Wciśnij klawisz skrótu w dowolnej grze lub na pulpicie. Nagrywanie z dźwiękiem działa w tle.",
                HeroButton = "⚡  Zapisz klip teraz",

                SidebarStatusActive = "Bufor aktywny (NVENC)",
                SidebarStatusPaused = "Bufor wstrzymany",
                SidebarInfoFormat = "Skrót: [{0}] • Bufor: {1}s",

                ClipsTitle = "Zapisane powtórki",
                ClipsOpenFolder = "📁  Otwórz folder z klipami",
                ClipsEmpty = "Brak nagranych klipów. Wciśnij {0} podczas gry, aby zapisać powtórkę.",
                ClipsPlay = "▶ Odtwórz",
                ClipsDelete = "🗑 Usuń",
                ClipsCopyPath = "📋 Kopiuj ścieżkę",

                VideoScreenTitle = "Monitor do przechwytywania",
                VideoScreenSub = "Wybierz ekran, z którego rejestrowane są powtórki.",
                ScreenLabel = "Ekran",
                ScreenPrimaryBadge = "Główny",
                ScreenPrimaryDefault = "Główny monitor systemowy",

                VideoBufferTitle = "Aktywny bufor w pamięci",
                VideoBufferSub = "Ciągłe nagrywanie w RAM. Po wciśnięciu skrótu powtórka zapisuje się natychmiast.",
                VideoDurationTitle = "Długość powtórki (bufor)",
                VideoDurationSub = "Czas rejestrowany wstecz przy zapisie powtórki.",
                VideoFpsTitle = "Płynność nagrania (FPS)",
                VideoFpsSub = "Liczba klatek na sekundę dla przechwytywania wideo.",
                VideoBitrateTitle = "Jakość wideo (Bitrate)",
                VideoBitrateSub = "Przepływność strumienia wideo kodera GPU.",
                VideoCursorTitle = "Nagrywaj kursor myszy",
                VideoCursorSub = "Widoczność wskaźnika myszy na nagraniach powtórek.",
                VideoFolderTitle = "Folder zapisu powtórek",
                VideoFolderSub = "Katalog, w którym zapisywane są gotowe pliki MP4.",
                VideoFolderBtn = "Zmień folder...",

                AudioPlaySoundTitle = "Dźwięk potwierdzenia zapisu",
                AudioPlaySoundSub = "Krótki sygnał audio potwierdzający przechwycenie powtórki.",
                AudioPresetTitle = "Wybór profilu dźwiękowego",
                AudioPresetSub = "Proceduralne efekty dźwiękowe generowane w pamięci RAM.",
                AudioTestBtn = "▶ Testuj dźwięk",
                AudioCustomTitle = "Własny plik audio",
                AudioCustomSub = "Wybierz własny plik .wav lub .mp3 do odtworzenia po zapisie klipu.",
                AudioCustomBtn = "Wybierz plik...",
                AudioSysVolTitle = "Głośność dźwięku z gry i systemu",
                AudioSysVolSub = "WASAPI 48kHz Loopback z domyślnego urządzenia wyjściowego.",
                AudioMicVolTitle = "Głośność mikrofonu",
                AudioMicVolSub = "Rejestrowanie Twojego głosu w czasie rzeczywistym.",
                AudioMicDeviceTitle = "Urządzenie mikrofonowe",
                AudioMicDeviceSub = "Wybierz mikrofon, który ma być nagrywany.",

                HotkeyTitle = "Klawisz zapisu powtórki",
                HotkeySub = "Globalny skrót klawiszowy działający we wszystkich grach pełnoekranowych.",

                SystemLangTitle = "Język aplikacji",
                SystemLangSub = "Wybierz język interfejsu programu Rewind.",
                SystemAutostartTitle = "Uruchamiaj z systemem Windows",
                SystemAutostartSub = "Automatyczne uruchamianie przy starcie komputera.",
                SystemStartMinTitle = "Uruchamiaj zminimalizowany",
                SystemStartMinSub = "Start programu bez otwierania okna, bezpośrednio w zasobniku.",
                SystemHudTitle = "Powiadomienie HUD po zapisie",
                SystemHudSub = "Dyskretny baner po zapisaniu powtórki z szybkim dostępem do pliku.",
                SystemThemeTitle = "Motyw interfejsu (Ciemny / Jasny)",
                SystemThemeSub = "Przełącz między głębokim ciemnym a czystym jasnym stylem.",

                AboutVersionTitle = "Wersja aplikacji",
                AboutVersionSub = "Samodzielny plik all-in-one bez żadnych zewnętrznych zależności.",
                AboutGpuTitle = "Koder graficzny GPU",
                AboutGpuSub = "Natywny akcelerator sprzętowy h264_nvenc z zerowym opóźnieniem.",
                AboutAudioTitle = "Silnik audio",
                AboutAudioSub = "WASAPI Loopback 48kHz Stereo + mikser mikrofonowy w czasie rzeczywistym.",
                AboutFootprintTitle = "Narzut zasobów na system",
                AboutFootprintSub = "Zminimalizowana praca procesora oraz pamięci RAM podczas grania.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Bufor aktywny (NVENC)",
                TrayBufferPaused = "    ○ Bufor wstrzymany",
                TraySaveClip = "⚡  Zapisz powtórkę",
                TrayOpenFolder = "📁  Otwórz folder z klipami",
                TrayPauseBuffer = "Wstrzymaj bufor",
                TrayResumeBuffer = "Wznów bufor",
                TraySettings = "⚙️  Ustawienia Rewind",
                TrayExit = "❌  Wyjdź z programu",

                HudTitleFormat = "Zapisano klip ({0}s)",
                HudSubtitle = "Powtórka gotowa w folderze klipów.",
                HudOpenBtn = "Otwórz folder",
                BalloonTitle = "Rewind gotowy w zasobniku",
                BalloonSub = "Bufor powtórek aktywny w tle. Wciśnij {0}, aby zapisać klip.",

                ClipsRevealBtn = "Folder",
                ClipsDeleteConfirm = "Czy na pewno chcesz usunąć klip {0}?",
                ClipsDeleteTitle = "Usuń klip",

                SectionHeaderVideo = "Wideo i bufor powtórek",
                SectionHeaderVideoSub = "Skonfiguruj ekran, długość powtórki, klatkaż oraz sprzętowy enkoder GPU.",
                SectionHeaderAudio = "Dźwięk i mikser audio",
                SectionHeaderAudioSub = "Konfiguracja nagrywania dźwięku z gry, mikrofonu oraz personalizacja dźwięku powtórki.",
                SectionHeaderHotkeys = "Skróty klawiszowe",
                SectionHeaderHotkeysSub = "Globalne klawisze wyzwalające natychmiastowe przechwycenie powtórki.",
                SectionHeaderSystem = "System, wygląd i powiadomienia",
                SectionHeaderSystemSub = "Personalizacja wyglądu aplikacji, powiadomień HUD oraz autostartu z Windows.",

                VideoGpuTitle = "Sprzętowa akceleracja kodowania",
                VideoGpuSub = "Koder GPU odciąża procesor CPU do < 0.5%, gwarantując zero spadków FPS w grach.",
                HotkeyGlobalTitle = "Tryb globalnego nasłuchu",
                HotkeyGlobalSub = "Skrót jest rejestrowany na poziomie systemu Windows za pośrednictwem RegisterHotKey.",
                HotkeyBadgeText = "✓ Działa w każdej grze pełnoekranowej",
                SystemBufferActivityTitle = "Aktywność bufora w tle",
                SystemBufferActivitySub = "Możesz tymczasowo zatrzymać koder wideo, gdy nie planujesz zapisywać żadnych powtórek.",

                AudioMicDefault = "Domyślny mikrofon systemowy",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Migawka",
                SoundPresetDing = "Dzwonek",
                SoundPresetRetro = "Retro 8-bit",
                SoundPresetCustom = "Własny plik",
                SoundCustomEmpty = "Brak wybranego pliku",

                ToastClipSaved = "🎬 Klip został pomyślnie zapisany!",
                ToastPlayNow = "Odtwórz teraz",
                TooltipLightMode = "Przełącz na tryb jasny",
                TooltipDarkMode = "Przełącz na tryb ciemny",
                TooltipMinimize = "Zminimalizuj do zasobnika",
                TooltipClose = "Schowaj do zasobnika",
                FooterEscHint = "Wciśnij [Esc], aby schować do traya"
            };

            // 2. English
            _dict["en"] = new UiStrings {
                NavClips = "🎬  My Replays",
                NavVideo = "📹  Video & Buffer",
                NavAudio = "🎧  Audio & Mixer",
                NavHotkeys = "⌨️  Hotkeys",
                NavSystem = "⚙️  System & Startup",
                NavAbout = "ℹ️  About Rewind",

                HeroHotkeyPrefix = "HOTKEY: ",
                HeroTitleFormat = "Save last {0} seconds",
                HeroSubtitle = "Press the hotkey in any game or desktop. High-speed in-memory recording runs silently in the background.",
                HeroButton = "⚡  Save Clip Now",

                SidebarStatusActive = "Buffer active (NVENC)",
                SidebarStatusPaused = "Buffer paused",
                SidebarInfoFormat = "Hotkey: [{0}] • Buffer: {1}s",

                ClipsTitle = "Saved Replays",
                ClipsOpenFolder = "📁  Open Clips Folder",
                ClipsEmpty = "No clips recorded yet. Press {0} during gameplay to capture a replay.",
                ClipsPlay = "▶ Play",
                ClipsDelete = "🗑 Delete",
                ClipsCopyPath = "📋 Copy Path",

                VideoScreenTitle = "Display to Capture",
                VideoScreenSub = "Select which monitor to record gameplay replays from.",
                ScreenLabel = "Display",
                ScreenPrimaryBadge = "Primary",
                ScreenPrimaryDefault = "Primary System Display",

                VideoBufferTitle = "Active in-memory buffer",
                VideoBufferSub = "Continuous circular recording in RAM. Clips save instantly upon hotkey press.",
                VideoDurationTitle = "Replay buffer duration",
                VideoDurationSub = "Time window captured retrospectively when triggering clip.",
                VideoFpsTitle = "Capture Framerate (FPS)",
                VideoFpsSub = "Framerate for high-motion gameplay recording.",
                VideoBitrateTitle = "Video Bitrate",
                VideoBitrateSub = "Encoding bitrate for hardware GPU pipeline.",
                VideoCursorTitle = "Capture mouse cursor",
                VideoCursorSub = "Show mouse pointer on saved video clips.",
                VideoFolderTitle = "Clips destination folder",
                VideoFolderSub = "Directory where MP4 video replays are stored.",
                VideoFolderBtn = "Change Folder...",

                AudioPlaySoundTitle = "Capture confirmation sound",
                AudioPlaySoundSub = "Short acoustic cue confirming a replay has been saved.",
                AudioPresetTitle = "Sound effect preset",
                AudioPresetSub = "Procedural sound effects synthesized in memory with zero disk delay.",
                AudioTestBtn = "▶ Test Sound",
                AudioCustomTitle = "Custom audio file",
                AudioCustomSub = "Select your own .wav or .mp3 file to play when saving clips.",
                AudioCustomBtn = "Select file...",
                AudioSysVolTitle = "Game & system audio volume",
                AudioSysVolSub = "WASAPI 48kHz Loopback from your default output device.",
                AudioMicVolTitle = "Microphone volume",
                AudioMicVolSub = "Real-time microphone input captured alongside game audio.",
                AudioMicDeviceTitle = "Microphone device",
                AudioMicDeviceSub = "Select the microphone device to capture.",

                HotkeyTitle = "Save replay hotkey",
                HotkeySub = "Global system-wide hotkey working across all full-screen games.",

                SystemLangTitle = "Application Language",
                SystemLangSub = "Select the user interface language for Rewind.",
                SystemAutostartTitle = "Start with Windows",
                SystemAutostartSub = "Automatically launch Rewind on system boot.",
                SystemStartMinTitle = "Start minimized",
                SystemStartMinSub = "Launch silently directly into the system tray without opening window.",
                SystemHudTitle = "HUD overlay notification",
                SystemHudSub = "Non-activating discreet toast banner displayed upon saving clips.",
                SystemThemeTitle = "Interface Theme (Dark / Light)",
                SystemThemeSub = "Switch between pitch-black and clean light visual modes.",

                AboutVersionTitle = "Application Version",
                AboutVersionSub = "Self-contained standalone executable with zero external dependencies.",
                AboutGpuTitle = "Hardware GPU Encoder",
                AboutGpuSub = "Native zero-latency hardware acceleration via h264_nvenc.",
                AboutAudioTitle = "Audio Engine",
                AboutAudioSub = "WASAPI Studio 48kHz Stereo Loopback with real-time mic mixer.",
                AboutFootprintTitle = "System Resource Overhead",
                AboutFootprintSub = "Ultra-low CPU overhead (<0.5%) and fixed ~80 MB RAM ceiling.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Buffer active (NVENC)",
                TrayBufferPaused = "    ○ Buffer paused",
                TraySaveClip = "⚡  Save Replay",
                TrayOpenFolder = "📁  Open Clips Folder",
                TrayPauseBuffer = "Pause Buffer",
                TrayResumeBuffer = "Resume Buffer",
                TraySettings = "⚙️  Rewind Settings",
                TrayExit = "❌  Exit Rewind",

                HudTitleFormat = "Clip Saved ({0}s)",
                HudSubtitle = "Replay is ready in your clips folder.",
                HudOpenBtn = "Open Folder",
                BalloonTitle = "Rewind active in system tray",
                BalloonSub = "Replay buffer running in background. Press {0} to save a clip.",

                ClipsRevealBtn = "Folder",
                ClipsDeleteConfirm = "Are you sure you want to delete clip {0}?",
                ClipsDeleteTitle = "Delete Clip",

                SectionHeaderVideo = "Video & Buffer Settings",
                SectionHeaderVideoSub = "Configure display, replay length, framerate, and GPU hardware encoder.",
                SectionHeaderAudio = "Audio & Mixer Settings",
                SectionHeaderAudioSub = "Configure game audio loopback, microphone input, and replay sound effects.",
                SectionHeaderHotkeys = "Hotkeys & Shortcuts",
                SectionHeaderHotkeysSub = "Global key combinations to trigger instant replay clips.",
                SectionHeaderSystem = "System, Appearance & Startup",
                SectionHeaderSystemSub = "Customize theme appearance, gaming HUD toast, and Windows autostart.",

                VideoGpuTitle = "Hardware-accelerated encoding",
                VideoGpuSub = "GPU encoder offloads the CPU to < 0.5%, ensuring zero in-game FPS drops.",
                HotkeyGlobalTitle = "Global hook mode",
                HotkeyGlobalSub = "Shortcut is registered system-wide across all full-screen applications.",
                HotkeyBadgeText = "✓ Active in all fullscreen games",
                SystemBufferActivityTitle = "Background buffer state",
                SystemBufferActivitySub = "Temporarily pause video capture when you do not intend to record clips.",

                AudioMicDefault = "Default system microphone",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Shutter",
                SoundPresetDing = "Chime",
                SoundPresetRetro = "Retro 8-bit",
                SoundPresetCustom = "Custom file",
                SoundCustomEmpty = "No file selected",

                ToastClipSaved = "🎬 Replay clip saved successfully!",
                ToastPlayNow = "Play now",
                TooltipLightMode = "Switch to Light Mode",
                TooltipDarkMode = "Switch to Dark Mode",
                TooltipMinimize = "Minimize to tray",
                TooltipClose = "Hide to tray",
                FooterEscHint = "Press [Esc] to minimize to tray"
            };

            // 3. Deutsch
            _dict["de"] = new UiStrings {
                NavClips = "🎬  Meine Replays",
                NavVideo = "📹  Video & Puffer",
                NavAudio = "🎧  Audio & Mixer",
                NavHotkeys = "⌨️  Tastenkombinationen",
                NavSystem = "⚙️  System & Autostart",
                NavAbout = "ℹ️  Über Rewind",

                HeroHotkeyPrefix = "TASTE: ",
                HeroTitleFormat = "Letzte {0} Sekunden speichern",
                HeroSubtitle = "Drücken Sie die Tastenkombination im Spiel. Die Aufnahme läuft geräuschlos im Hintergrund.",
                HeroButton = "⚡  Clip jetzt speichern",

                SidebarStatusActive = "Puffer aktiv (NVENC)",
                SidebarStatusPaused = "Puffer pausiert",
                SidebarInfoFormat = "Taste: [{0}] • Puffer: {1}s",

                ClipsTitle = "Gespeicherte Replays",
                ClipsOpenFolder = "📁  Ordner öffnen",
                ClipsEmpty = "Noch keine Clips vorhanden. Drücken Sie {0} im Spiel.",
                ClipsPlay = "▶ Abspielen",
                ClipsDelete = "🗑 Löschen",
                ClipsCopyPath = "📋 Pfad kopieren",

                VideoScreenTitle = "Aufzunehmender Bildschirm",
                VideoScreenSub = "Wählen Sie den Monitor für Spielaufnahmen aus.",
                ScreenLabel = "Bildschirm",
                ScreenPrimaryBadge = "Haupt",
                ScreenPrimaryDefault = "Primärer Systembildschirm",

                VideoBufferTitle = "Aktiver Speicherpuffer",
                VideoBufferSub = "Kontinuierliche Aufzeichnung im RAM ohne Festplattenverschleiß.",
                VideoDurationTitle = "Pufferdauer (Replay)",
                VideoDurationSub = "Rückwirkend aufgenommene Zeitdauer beim Speichern.",
                VideoFpsTitle = "Bildwiederholrate (FPS)",
                VideoFpsSub = "Bildrate für die flüssige Spielaufnahme.",
                VideoBitrateTitle = "Video-Bitrate",
                VideoBitrateSub = "Kodierungs-Bitrate für den GPU-Hardware-Encoder.",
                VideoCursorTitle = "Mauszeiger aufnehmen",
                VideoCursorSub = "Mauszeiger in den gespeicherten Clips anzeigen.",
                VideoFolderTitle = "Speicherordner für Clips",
                VideoFolderSub = "Verzeichnis für MP4-Videodateien.",
                VideoFolderBtn = "Ordner ändern...",

                AudioPlaySoundTitle = "Bestätigungston beim Speichern",
                AudioPlaySoundSub = "Kurzer Ton zur Bestätigung der Clip-Aufnahme.",
                AudioPresetTitle = "Soundeffekt-Profil",
                AudioPresetSub = "Prozedural im RAM erzeugte Klangeffekte ohne Verzögerung.",
                AudioTestBtn = "▶ Ton testen",
                AudioCustomTitle = "Eigene Audiodatei",
                AudioCustomSub = "Wählen Sie eine .wav- oder .mp3-Datei aus.",
                AudioCustomBtn = "Datei wählen...",
                AudioSysVolTitle = "Spiel- und Systemlautstärke",
                AudioSysVolSub = "WASAPI 48kHz Loopback vom Standard-Ausgabegerät.",
                AudioMicVolTitle = "Mikrofonlautstärke",
                AudioMicVolSub = "Echtzeit-Mikrofonaufnahme neben dem Spielton.",
                AudioMicDeviceTitle = "Mikrofon-Gerät",
                AudioMicDeviceSub = "Wählen Sie das aufzunehmende Mikrofon.",

                HotkeyTitle = "Tastenkombination zum Speichern",
                HotkeySub = "Globaler Hotkey funktioniert in allen Vollbildspielen.",

                SystemLangTitle = "Sprache der Anwendung",
                SystemLangSub = "Wählen Sie die Benutzeroberflächensprache für Rewind.",
                SystemAutostartTitle = "Mit Windows starten",
                SystemAutostartSub = "Rewind beim Hochfahren des Computers automatisch starten.",
                SystemStartMinTitle = "Minimiert starten",
                SystemStartMinSub = "Direkt im Infobereich starten ohne Fenster zu öffnen.",
                SystemHudTitle = "HUD-Benachrichtigung",
                SystemHudSub = "Dezentes Overlay nach dem Speichern eines Clips.",
                SystemThemeTitle = "Design (Dunkel / Hell)",
                SystemThemeSub = "Umschalten zwischen dunklem und hellem Modus.",

                AboutVersionTitle = "Anwendungsversion",
                AboutVersionSub = "Eigenständige All-in-One-Anwendung ohne externe Abhängigkeiten.",
                AboutGpuTitle = "GPU-Hardware-Encoder",
                AboutGpuSub = "Natives h264_nvenc mit minimaler Latenz.",
                AboutAudioTitle = "Audiosystem",
                AboutAudioSub = "WASAPI 48kHz Stereo Loopback mit Echtzeit-Mischer.",
                AboutFootprintTitle = "Ressourcenverbrauch",
                AboutFootprintSub = "Minimale CPU-Auslastung (<0.5%) und feste ~80 MB RAM-Nutzung.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Puffer aktiv (NVENC)",
                TrayBufferPaused = "    ○ Puffer pausiert",
                TraySaveClip = "⚡  Replay speichern",
                TrayOpenFolder = "📁  Ordner öffnen",
                TrayPauseBuffer = "Puffer pausieren",
                TrayResumeBuffer = "Puffer fortsetzen",
                TraySettings = "⚙️  Rewind Einstellungen",
                TrayExit = "❌  Beenden",

                HudTitleFormat = "Clip gespeichert ({0}s)",
                HudSubtitle = "Replay im Ordner verfügbar.",
                HudOpenBtn = "Ordner öffnen",
                BalloonTitle = "Rewind im Infobereich bereit",
                BalloonSub = "Puffer aktiv. Drücken Sie {0}, um einen Clip zu speichern.",

                ClipsRevealBtn = "Ordner",
                ClipsDeleteConfirm = "Möchten Sie den Clip {0} wirklich löschen?",
                ClipsDeleteTitle = "Clip löschen",

                SectionHeaderVideo = "Video & Puffer-Einstellungen",
                SectionHeaderVideoSub = "Konfigurieren Sie Bildschirm, Pufferdauer, Bildrate und GPU-Encoder.",
                SectionHeaderAudio = "Audio & Mixer-Einstellungen",
                SectionHeaderAudioSub = "Spielsound, Mikrofoneingang und Bestätigungstöne einrichten.",
                SectionHeaderHotkeys = "Tastatur-Hotkeys",
                SectionHeaderHotkeysSub = "Globale Tasten zum sofortigen Auslösen von Replays.",
                SectionHeaderSystem = "System, Design & Autostart",
                SectionHeaderSystemSub = "Erscheinungsbild, HUD-Overlay und Windows-Autostart anpassen.",

                VideoGpuTitle = "Hardwarebeschleunigte Codierung",
                VideoGpuSub = "Der GPU-Encoder entlastet die CPU auf < 0.5% für ruckelfreies Gaming.",
                HotkeyGlobalTitle = "Globaler Tastatur-Hook",
                HotkeyGlobalSub = "Der Hotkey ist systemweit in allen Vollbildanwendungen registriert.",
                HotkeyBadgeText = "✓ Funktioniert in allen Vollbildspielen",
                SystemBufferActivityTitle = "Puffer-Hintergrundaktivität",
                SystemBufferActivitySub = "Pausieren Sie den Video-Encoder, wenn keine Aufnahmen benötigt werden.",

                AudioMicDefault = "Standard-Systemmikrofon",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Verschluss",
                SoundPresetDing = "Glocke",
                SoundPresetRetro = "Retro 8-Bit",
                SoundPresetCustom = "Eigene Datei",
                SoundCustomEmpty = "Keine Datei ausgewählt",

                ToastClipSaved = "🎬 Clip erfolgreich gespeichert!",
                ToastPlayNow = "Jetzt abspielen",
                TooltipLightMode = "Zu hellem Modus wechseln",
                TooltipDarkMode = "Zu dunklem Modus wechseln",
                TooltipMinimize = "In den Infobereich minimieren",
                TooltipClose = "In den Infobereich ausblenden",
                FooterEscHint = "Drücken Sie [Esc], um ins Tray zu minimieren"
            };

            // 4. Español
            _dict["es"] = new UiStrings {
                NavClips = "🎬  Mis Repeticiones",
                NavVideo = "📹  Vídeo y Búfer",
                NavAudio = "🎧  Audio y Mezclador",
                NavHotkeys = "⌨️  Atajos de Teclado",
                NavSystem = "⚙️  Sistema e Inicio",
                NavAbout = "ℹ️  Acerca de Rewind",

                HeroHotkeyPrefix = "ATAJO: ",
                HeroTitleFormat = "Guardar últimos {0} segundos",
                HeroSubtitle = "Presiona el atajo en cualquier juego o escritorio. Grabación en memoria activa en segundo plano.",
                HeroButton = "⚡  Guardar clip ahora",

                SidebarStatusActive = "Búfer activo (NVENC)",
                SidebarStatusPaused = "Búfer en pausa",
                SidebarInfoFormat = "Atajo: [{0}] • Búfer: {1}s",

                ClipsTitle = "Repeticiones Guardadas",
                ClipsOpenFolder = "📁  Abrir carpeta de clips",
                ClipsEmpty = "Aún no hay clips guardados. Presiona {0} en el juego.",
                ClipsPlay = "▶ Reproducir",
                ClipsDelete = "🗑 Eliminar",
                ClipsCopyPath = "📋 Copiar ruta",

                VideoScreenTitle = "Pantalla para capturar",
                VideoScreenSub = "Selecciona qué monitor grabar en partidas.",
                ScreenLabel = "Pantalla",
                ScreenPrimaryBadge = "Principal",
                ScreenPrimaryDefault = "Pantalla principal del sistema",

                VideoBufferTitle = "Búfer activo en memoria",
                VideoBufferSub = "Grabación circular en RAM. Los clips se guardan al instante.",
                VideoDurationTitle = "Duración de la repetición",
                VideoDurationSub = "Tiempo capturado hacia atrás al pulsar el atajo.",
                VideoFpsTitle = "Fotogramas por segundo (FPS)",
                VideoFpsSub = "Fluidez para grabación de juegos.",
                VideoBitrateTitle = "Bitrate de vídeo",
                VideoBitrateSub = "Tasa de bits para el codificador GPU por hardware.",
                VideoCursorTitle = "Capturar cursor del ratón",
                VideoCursorSub = "Mostrar el puntero del ratón en las repeticiones.",
                VideoFolderTitle = "Carpeta de destino de clips",
                VideoFolderSub = "Directorio donde se guardan los archivos MP4.",
                VideoFolderBtn = "Cambiar carpeta...",

                AudioPlaySoundTitle = "Sonido de confirmación",
                AudioPlaySoundSub = "Señal de audio que confirma la captura del clip.",
                AudioPresetTitle = "Perfil de efecto de sonido",
                AudioPresetSub = "Efectos sintetizados en RAM con latencia cero.",
                AudioTestBtn = "▶ Probar sonido",
                AudioCustomTitle = "Archivo de audio personalizado",
                AudioCustomSub = "Selecciona un archivo .wav o .mp3 propio.",
                AudioCustomBtn = "Seleccionar archivo...",
                AudioSysVolTitle = "Volumen del juego y sistema",
                AudioSysVolSub = "WASAPI 48kHz Loopback desde el dispositivo predeterminado.",
                AudioMicVolTitle = "Volumen del micrófono",
                AudioMicVolSub = "Entrada de micrófono en tiempo real.",
                AudioMicDeviceTitle = "Dispositivo de micrófono",
                AudioMicDeviceSub = "Elige el micrófono que deseas capturar.",

                HotkeyTitle = "Atajo para guardar repetición",
                HotkeySub = "Atajo global activo en todos los juegos a pantalla completa.",

                SystemLangTitle = "Idioma de la aplicación",
                SystemLangSub = "Selecciona el idioma de la interfaz de Rewind.",
                SystemAutostartTitle = "Iniciar con Windows",
                SystemAutostartSub = "Iniciar Rewind automáticamente al encender el PC.",
                SystemStartMinTitle = "Iniciar minimizado",
                SystemStartMinSub = "Iniciar directamente en la bandeja sin abrir la ventana.",
                SystemHudTitle = "Notificación HUD en pantalla",
                SystemHudSub = "Aviso discreto sin robar foco al guardar un clip.",
                SystemThemeTitle = "Tema visual (Oscuro / Claro)",
                SystemThemeSub = "Cambiar entre modo oscuro profundo y modo claro limpio.",

                AboutVersionTitle = "Versión de la aplicación",
                AboutVersionSub = "Archivo único independiente sin dependencias externas.",
                AboutGpuTitle = "Codificador GPU por hardware",
                AboutGpuSub = "Aceleración nativa h264_nvenc con latencia cero.",
                AboutAudioTitle = "Motor de audio",
                AboutAudioSub = "WASAPI 48kHz Estéreo con mezclador en tiempo real.",
                AboutFootprintTitle = "Consumo de recursos",
                AboutFootprintSub = "Uso mínimo de CPU (<0.5%) y memoria RAM fija en ~80 MB.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Búfer activo (NVENC)",
                TrayBufferPaused = "    ○ Búfer en pausa",
                TraySaveClip = "⚡  Guardar repetición",
                TrayOpenFolder = "📁  Abrir carpeta de clips",
                TrayPauseBuffer = "Pausar búfer",
                TrayResumeBuffer = "Reanudar búfer",
                TraySettings = "⚙️  Ajustes de Rewind",
                TrayExit = "❌  Salir",

                HudTitleFormat = "Clip guardado ({0}s)",
                HudSubtitle = "Repetición lista en la carpeta de clips.",
                HudOpenBtn = "Abrir carpeta",
                BalloonTitle = "Rewind listo en la bandeja",
                BalloonSub = "Búfer activo. Presiona {0} para guardar una repetición.",

                ClipsRevealBtn = "Carpeta",
                ClipsDeleteConfirm = "¿Seguro que quieres eliminar el clip {0}?",
                ClipsDeleteTitle = "Eliminar clip",

                SectionHeaderVideo = "Ajustes de vídeo y búfer",
                SectionHeaderVideoSub = "Configura pantalla, duración, fotogramas y codificador GPU.",
                SectionHeaderAudio = "Ajustes de audio y mezclador",
                SectionHeaderAudioSub = "Configura el sonido del juego, micrófono y avisos acústicos.",
                SectionHeaderHotkeys = "Atajos de teclado",
                SectionHeaderHotkeysSub = "Combinaciones globales para capturar clips al instante.",
                SectionHeaderSystem = "Sistema, apariencia e inicio",
                SectionHeaderSystemSub = "Personaliza el tema, aviso HUD e inicio automático con Windows.",

                VideoGpuTitle = "Aceleración de codificación por hardware",
                VideoGpuSub = "El codificador GPU reduce la carga de CPU a < 0.5% sin caídas de FPS.",
                HotkeyGlobalTitle = "Modo de captura global",
                HotkeyGlobalSub = "El atajo está registrado a nivel del sistema para todos los juegos.",
                HotkeyBadgeText = "✓ Funciona en cualquier juego a pantalla completa",
                SystemBufferActivityTitle = "Actividad del búfer en segundo plano",
                SystemBufferActivitySub = "Pausa temporalmente el codificador de vídeo cuando no desees grabar.",

                AudioMicDefault = "Micrófono predeterminado del sistema",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Obturador",
                SoundPresetDing = "Campana",
                SoundPresetRetro = "Retro 8-bit",
                SoundPresetCustom = "Archivo propio",
                SoundCustomEmpty = "Ningún archivo seleccionado",

                ToastClipSaved = "🎬 ¡Clip guardado con éxito!",
                ToastPlayNow = "Reproducir ahora",
                TooltipLightMode = "Cambiar a modo claro",
                TooltipDarkMode = "Cambiar a modo oscuro",
                TooltipMinimize = "Minimizar a la bandeja",
                TooltipClose = "Ocultar en la bandeja",
                FooterEscHint = "Presiona [Esc] para ocultar en la bandeja"
            };

            // 5. Français
            _dict["fr"] = new UiStrings {
                NavClips = "🎬  Mes Rediffusions",
                NavVideo = "📹  Vidéo & Mémoire",
                NavAudio = "🎧  Audio & Mixeur",
                NavHotkeys = "⌨️  Raccourcis",
                NavSystem = "⚙️  Système & Démarrage",
                NavAbout = "ℹ️  À propos de Rewind",

                HeroHotkeyPrefix = "RACCOURCI : ",
                HeroTitleFormat = "Sauvegarder les {0} dernières secondes",
                HeroSubtitle = "Appuyez sur le raccourci dans n'importe quel jeu. Enregistrement en mémoire actif en arrière-plan.",
                HeroButton = "⚡  Sauvegarder le clip",

                SidebarStatusActive = "Mémoire active (NVENC)",
                SidebarStatusPaused = "Mémoire en pause",
                SidebarInfoFormat = "Raccourci : [{0}] • Mémoire : {1}s",

                ClipsTitle = "Rediffusions Enregistrées",
                ClipsOpenFolder = "📁  Ouvrir le dossier",
                ClipsEmpty = "Aucun clip enregistré pour le moment. Appuyez sur {0} en jeu.",
                ClipsPlay = "▶ Lire",
                ClipsDelete = "🗑 Supprimer",
                ClipsCopyPath = "📋 Copier le chemin",

                VideoScreenTitle = "Écran à capturer",
                VideoScreenSub = "Sélectionnez le moniteur à enregistrer pour les replays.",
                ScreenLabel = "Écran",
                ScreenPrimaryBadge = "Principal",
                ScreenPrimaryDefault = "Écran principal du système",

                VideoBufferTitle = "Mémoire tampon active",
                VideoBufferSub = "Enregistrement circulaire en RAM sans usure du disque.",
                VideoDurationTitle = "Durée de rediffusion",
                VideoDurationSub = "Période enregistrée rétrospectivement lors de la capture.",
                VideoFpsTitle = "Images par seconde (FPS)",
                VideoFpsSub = "Fluidité d'enregistrement pour les jeux rapides.",
                VideoBitrateTitle = "Débit vidéo (Bitrate)",
                VideoBitrateSub = "Débit d'encodage pour le GPU matériel.",
                VideoCursorTitle = "Capturer le curseur",
                VideoCursorSub = "Afficher le curseur de la souris dans les clips.",
                VideoFolderTitle = "Dossier de destination",
                VideoFolderSub = "Répertoire où sont sauvegardés les fichiers MP4.",
                VideoFolderBtn = "Changer de dossier...",

                AudioPlaySoundTitle = "Son de confirmation",
                AudioPlaySoundSub = "Bip audio confirmant la capture du clip.",
                AudioPresetTitle = "Profil d'effet sonore",
                AudioPresetSub = "Effets procéduraux en mémoire sans latence disque.",
                AudioTestBtn = "▶ Tester le son",
                AudioCustomTitle = "Fichier audio personnalisé",
                AudioCustomSub = "Sélectionnez un fichier .wav ou .mp3 de votre choix.",
                AudioCustomBtn = "Choisir un fichier...",
                AudioSysVolTitle = "Volume du jeu et du système",
                AudioSysVolSub = "WASAPI 48kHz Loopback du périphérique par défaut.",
                AudioMicVolTitle = "Volume du microphone",
                AudioMicVolSub = "Enregistrement vocal en temps réel.",
                AudioMicDeviceTitle = "Périphérique microphone",
                AudioMicDeviceSub = "Sélectionnez le micro à capturer.",

                HotkeyTitle = "Raccourci de capture",
                HotkeySub = "Raccourci global fonctionnant dans tous les jeux plein écran.",

                SystemLangTitle = "Langue de l'application",
                SystemLangSub = "Choisissez la langue de l'interface de Rewind.",
                SystemAutostartTitle = "Démarrer avec Windows",
                SystemAutostartSub = "Lancer Rewind automatiquement au démarrage du PC.",
                SystemStartMinTitle = "Démarrer réduit",
                SystemStartMinSub = "Lancer directement dans la barre des tâches sans ouvrir la fenêtre.",
                SystemHudTitle = "Notification HUD à l'écran",
                SystemHudSub = "Bannière discrète sans perte de focus lors de la sauvegarde.",
                SystemThemeTitle = "Thème visuel (Sombre / Clair)",
                SystemThemeSub = "Basculer entre le mode sombre et le mode clair.",

                AboutVersionTitle = "Version de l'application",
                AboutVersionSub = "Fichier exécutable all-in-one sans dépendances externes.",
                AboutGpuTitle = "Encodeur matériel GPU",
                AboutGpuSub = "Accélération matérielle h264_nvenc à latence zéro.",
                AboutAudioTitle = "Moteur audio",
                AboutAudioSub = "WASAPI 48kHz Stéréo avec mixeur en temps réel.",
                AboutFootprintTitle = "Consommation système",
                AboutFootprintSub = "Charge CPU minimale (<0.5%) et RAM plafonnée à ~80 Mo.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Mémoire active (NVENC)",
                TrayBufferPaused = "    ○ Mémoire en pause",
                TraySaveClip = "⚡  Sauvegarder le clip",
                TrayOpenFolder = "📁  Ouvrir le dossier",
                TrayPauseBuffer = "Mettre en pause",
                TrayResumeBuffer = "Reprendre la mémoire",
                TraySettings = "⚙️  Paramètres de Rewind",
                TrayExit = "❌  Quitter",

                HudTitleFormat = "Clip sauvegardé ({0}s)",
                HudSubtitle = "Rediffusion disponible dans le dossier.",
                HudOpenBtn = "Ouvrir le dossier",
                BalloonTitle = "Rewind actif dans la barre des tâches",
                BalloonSub = "Mémoire active. Appuyez sur {0} pour enregistrer un clip.",

                ClipsRevealBtn = "Dossier",
                ClipsDeleteConfirm = "Voulez-vous vraiment supprimer le clip {0} ?",
                ClipsDeleteTitle = "Supprimer le clip",

                SectionHeaderVideo = "Paramètres vidéo et tampon",
                SectionHeaderVideoSub = "Configurez l'écran, la durée, le débit et l'encodeur GPU.",
                SectionHeaderAudio = "Paramètres audio et mixeur",
                SectionHeaderAudioSub = "Configurez le son du jeu, l'entrée micro et les signaux sonores.",
                SectionHeaderHotkeys = "Raccourcis clavier",
                SectionHeaderHotkeysSub = "Combinaisons globales pour déclencher des replays instantanés.",
                SectionHeaderSystem = "Système, apparence et démarrage",
                SectionHeaderSystemSub = "Personnalisez le thème, l'overlay HUD et le démarrage avec Windows.",

                VideoGpuTitle = "Accélération matérielle de l'encodage",
                VideoGpuSub = "L'encodeur GPU soulage le processeur à < 0.5% sans perte de FPS.",
                HotkeyGlobalTitle = "Mode de capture global",
                HotkeyGlobalSub = "Le raccourci est enregistré au niveau du système pour tous les jeux.",
                HotkeyBadgeText = "✓ Fonctionne dans tous les jeux plein écran",
                SystemBufferActivityTitle = "Activité du tampon en arrière-plan",
                SystemBufferActivitySub = "Mettez en pause l'encodeur vidéo lorsque vous n'enregistrez pas.",

                AudioMicDefault = "Microphone système par défaut",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Obturateur",
                SoundPresetDing = "Cloche",
                SoundPresetRetro = "Rétro 8-bit",
                SoundPresetCustom = "Fichier personnalisé",
                SoundCustomEmpty = "Aucun fichier sélectionné",

                ToastClipSaved = "🎬 Clip sauvegardé avec succès !",
                ToastPlayNow = "Lire maintenant",
                TooltipLightMode = "Passer au thème clair",
                TooltipDarkMode = "Passer au thème sombre",
                TooltipMinimize = "Réduire dans la zone de notification",
                TooltipClose = "Masquer dans la zone de notification",
                FooterEscHint = "Appuyez sur [Échap] pour masquer dans la zone de notification"
            };

            // 6. Українська
            _dict["ua"] = new UiStrings {
                NavClips = "🎬  Мої повтори",
                NavVideo = "📹  Відео та буфер",
                NavAudio = "🎧  Аудіо та мікшер",
                NavHotkeys = "⌨️  Гарячі клавіші",
                NavSystem = "⚙️  Система та автозапуск",
                NavAbout = "ℹ️  Про програму",

                HeroHotkeyPrefix = "СКОРОЧЕННЯ: ",
                HeroTitleFormat = "Зберегти останні {0} секунд",
                HeroSubtitle = "Натисніть гарячу клавішу в будь-якій грі або на робочому столі. Запис зі звуком працює у фоні.",
                HeroButton = "⚡  Зберегти кліп зараз",

                SidebarStatusActive = "Буфер активний (NVENC)",
                SidebarStatusPaused = "Буфер призупинено",
                SidebarInfoFormat = "Клавіша: [{0}] • Буфер: {1}с",

                ClipsTitle = "Збережені повтори",
                ClipsOpenFolder = "📁  Відкрити папку з кліпами",
                ClipsEmpty = "Ще немає збережених кліпів. Натисніть {0} під час гри.",
                ClipsPlay = "▶ Відтворити",
                ClipsDelete = "🗑 Видалити",
                ClipsCopyPath = "📋 Копіювати шлях",

                VideoScreenTitle = "Екран для захоплення",
                VideoScreenSub = "Оберіть монітор для запису ігрових повторів.",
                ScreenLabel = "Екран",
                ScreenPrimaryBadge = "Основний",
                ScreenPrimaryDefault = "Головний системний монітор",

                VideoBufferTitle = "Активний буфер у пам'яті",
                VideoBufferSub = "Безперервний круговий запис у RAM без зношування SSD.",
                VideoDurationTitle = "Тривалість повтору (буфер)",
                VideoDurationSub = "Час, що записується назад при збереженні кліпу.",
                VideoFpsTitle = "Частота кадрів (FPS)",
                VideoFpsSub = "Плавність для динамічного ігрового запису.",
                VideoBitrateTitle = "Бітрейт відео",
                VideoBitrateSub = "Швидкість потоку для апаратного GPU-кодувальника.",
                VideoCursorTitle = "Записувати курсор миші",
                VideoCursorSub = "Відображати вказівник миші на записах.",
                VideoFolderTitle = "Папка для збереження кліпів",
                VideoFolderSub = "Каталог, куди зберігаються готові MP4 файли.",
                VideoFolderBtn = "Змінити папку...",

                AudioPlaySoundTitle = "Звук підтвердження запису",
                AudioPlaySoundSub = "Короткий звуковий сигнал підтвердження збереження.",
                AudioPresetTitle = "Вибір звукового профілю",
                AudioPresetSub = "Процедурні звукові ефекти, створені в оперативній пам'яті.",
                AudioTestBtn = "▶ Тестувати звук",
                AudioCustomTitle = "Власний аудіофайл",
                AudioCustomSub = "Оберіть власний файл .wav або .mp3.",
                AudioCustomBtn = "Обрати файл...",
                AudioSysVolTitle = "Гучність гри та системи",
                AudioSysVolSub = "WASAPI 48kHz Loopback зі стандартного пристрою виводу.",
                AudioMicVolTitle = "Гучність мікрофона",
                AudioMicVolSub = "Запис вашого голосу в реальному часі.",
                AudioMicDeviceTitle = "Пристрій мікрофона",
                AudioMicDeviceSub = "Оберіть мікрофон для запису.",

                HotkeyTitle = "Гаряча клавіша збереження",
                HotkeySub = "Глобальна комбінація клавіш для всіх повноекранних ігор.",

                SystemLangTitle = "Мова програми",
                SystemLangSub = "Оберіть мову інтерфейсу Rewind.",
                SystemAutostartTitle = "Запускати разом з Windows",
                SystemAutostartSub = "Автоматичний запуск програми при старті ПК.",
                SystemStartMinTitle = "Запускати згорнутим",
                SystemStartMinSub = "Старт програми у треї без відкриття головного вікна.",
                SystemHudTitle = "HUD-сповіщення після запису",
                SystemHudSub = "Непомітний банер на екрані без втрати фокусу в іграх.",
                SystemThemeTitle = "Тема інтерфейсу (Темна / Світла)",
                SystemThemeSub = "Перемикання між темним та світлим оформленням.",

                AboutVersionTitle = "Версія програми",
                AboutVersionSub = "Автономний single-file виконуваний файл без сторонніх бібліотек.",
                AboutGpuTitle = "Апаратний GPU-кодувальник",
                AboutGpuSub = "Нативне апаратне прискорення h264_nvenc із нульовою затримкою.",
                AboutAudioTitle = "Звуковий рушій",
                AboutAudioSub = "WASAPI 48kHz Стерео з мікшером у реальному часі.",
                AboutFootprintTitle = "Навантаження на систему",
                AboutFootprintSub = "Мінімальне використання процесора (<0.5%) та ~80 МБ RAM.",

                TrayHeader = "Rewind v0.1",
                TrayBufferActive = "    ● Буфер активний (NVENC)",
                TrayBufferPaused = "    ○ Буфер призупинено",
                TraySaveClip = "⚡  Зберегти повтор",
                TrayOpenFolder = "📁  Відкрити папку з кліпами",
                TrayPauseBuffer = "Призупинити буфер",
                TrayResumeBuffer = "Відновити буфер",
                TraySettings = "⚙️  Налаштування Rewind",
                TrayExit = "❌  Вийти з програми",

                HudTitleFormat = "Кліп збережено ({0}с)",
                HudSubtitle = "Повтор готовий у папці з кліпами.",
                HudOpenBtn = "Відкрити папку",
                BalloonTitle = "Rewind активний у системному треї",
                BalloonSub = "Буфер працює у фоні. Натисніть {0}, щоб зберегти кліп.",

                ClipsRevealBtn = "Папка",
                ClipsDeleteConfirm = "Ви дійсно бажаєте видалити кліп {0}?",
                ClipsDeleteTitle = "Видалити кліп",

                SectionHeaderVideo = "Відео та параметри буфера",
                SectionHeaderVideoSub = "Налаштуйте екран, тривалість, частоту кадрів та GPU-кодер.",
                SectionHeaderAudio = "Аудіо та мікшер",
                SectionHeaderAudioSub = "Налаштування звуку гри, мікрофона та звукового підтвердження.",
                SectionHeaderHotkeys = "Гарячі клавіші",
                SectionHeaderHotkeysSub = "Глобальні клавіші для миттєвого збереження повторів.",
                SectionHeaderSystem = "Система, вигляд та автозапуск",
                SectionHeaderSystemSub = "Персоналізація теми, HUD-сповіщень та автозапуску з Windows.",

                VideoGpuTitle = "Апаратне прискорення кодування",
                VideoGpuSub = "GPU-кодер зменшує навантаження на процесор до < 0.5%, гарантуючи стабільний FPS.",
                HotkeyGlobalTitle = "Режим глобального перехоплення",
                HotkeyGlobalSub = "Комбінація реєструється в системі для всіх повноекранних ігор.",
                HotkeyBadgeText = "✓ Працює у будь-якій повноекранній грі",
                SystemBufferActivityTitle = "Активність фонового буфера",
                SystemBufferActivitySub = "Ви можете тимчасово зупинити відеокодер, коли не плануєте записувати повтори.",

                AudioMicDefault = "Головний системний мікрофон",
                SoundPresetDefault = "Rewind",
                SoundPresetShutter = "Затвор",
                SoundPresetDing = "Дзвіночок",
                SoundPresetRetro = "Ретро 8-біт",
                SoundPresetCustom = "Власний файл",
                SoundCustomEmpty = "Файл не обрано",

                ToastClipSaved = "🎬 Кліп успішно збережено!",
                ToastPlayNow = "Відтворити зараз",
                TooltipLightMode = "Перемкнути на світлу тему",
                TooltipDarkMode = "Перемкнути на темну тему",
                TooltipMinimize = "Згорнути в трей",
                TooltipClose = "Сховати в трей",
                FooterEscHint = "Натисніть [Esc], щоб сховати в трей"
            };
        }

        public static UiStrings Get(string langCode) {
            if (string.IsNullOrEmpty(langCode)) langCode = "pl";
            UiStrings s;
            if (_dict.TryGetValue(langCode, out s)) {
                return s;
            }
            return _dict["pl"];
        }

        public static Dictionary<string, string> AvailableLanguages {
            get {
                return new Dictionary<string, string> {
                    { "pl", "🇵🇱  Polski" },
                    { "en", "🇬🇧  English" },
                    { "de", "🇩🇪  Deutsch" },
                    { "es", "🇪🇸  Español" },
                    { "fr", "🇫🇷  Français" },
                    { "ua", "🇺🇦  Українська" }
                };
            }
        }
    }
}
