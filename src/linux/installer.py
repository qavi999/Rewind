#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Rewind Linux Graphical Installer (GUI Setup & CLI)
Sleek, modern windowed installer matching the Windows RewindSetup experience.
Contains embedded application payload; installs Rewind onto the system.
"""

import os
import sys
import time
import gzip
import base64
import shutil
import signal
import argparse
import subprocess
import threading
from pathlib import Path

try:
    from assets_b64 import LOGO_PNG_B64
except ImportError:
    LOGO_PNG_B64 = ""

PAYLOAD_GZ_B64 = ""

LANGUAGES = {
    "pl": {
        "name": "Polski", "flag": "🇵🇱",
        "lang_label": "Język aplikacji:",
        "title_setup": "Instalator Rewind v0.2",
        "title_uninst": "Dezinstalacja Rewind",
        "header_install_title": "Instalacja programu Rewind",
        "header_install_sub": "Błyskawiczne zapisywanie powtórek z gier i pulpitu (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Odinstaluj program Rewind",
        "header_uninst_sub": "Usunięcie programu, skrótów i konfiguracji.",
        "path_label": "Katalog instalacji programu:",
        "browse_btn": "Zmień...",
        "browse_dialog": "Wybierz folder do instalacji programu Rewind:",
        "opt_label": "Opcje skrótów i autostartu:",
        "cb_desktop": "Utwórz skrót na Pulpicie",
        "cb_menu": "Dodaj do Menu Aplikacji (Dash / Menu Start)",
        "cb_autostart": "Uruchamiaj z systemem Linux (autostart w tle)",
        "cb_launch": "Uruchom program Rewind po zakończeniu instalacji",
        "uninst_confirm": "Czy na pewno chcesz odinstalować program Rewind?\n\nWszystkie skróty z Pulpitu i Menu oraz pliki wykonywalne zostaną usunięte z Twojego komputera. Twoje nagrane klipy wideo pozostaną nienaruszone.",
        "btn_install": "Zainstaluj Rewind",
        "btn_uninst": "Odinstaluj program",
        "btn_finish": "Zakończ",
        "btn_close": "Zamknij",
        "prog_title_install": "Instalowanie programu...",
        "prog_title_uninst": "Odinstalowywanie programu...",
        "prog_closing": "Zamykanie aktywnych instancji programu...",
        "prog_dir": "Tworzenie katalogów aplikacji...",
        "prog_copy": "Wypakowywanie i instalowanie plików aplikacji...",
        "prog_shortcuts": "Tworzenie skrótów na Pulpicie i w Menu...",
        "prog_register": "Rejestracja w systemie Linux (aktualizacja bazy .desktop)...",
        "prog_done_install": "Instalacja ukończona pomyślnie!",
        "prog_done_uninst": "Dezinstalacja zakończona!",
        "finish_install_title": "Rewind został pomyślnie zainstalowany!",
        "finish_install_sub": "Domyślny skrót do tworzenia klipów: [F8]. Program działa w tle.",
        "finish_uninst_title": "Rewind został pomyślnie odinstalowany",
        "finish_uninst_sub": "Wszystkie pliki i skróty programu zostały usunięte.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    },
    "en": {
        "name": "English", "flag": "🇬🇧",
        "lang_label": "Application language:",
        "title_setup": "Rewind Setup v0.2",
        "title_uninst": "Uninstall Rewind",
        "header_install_title": "Install Rewind",
        "header_install_sub": "Instant replay buffer and game clipping engine (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Uninstall Rewind",
        "header_uninst_sub": "Remove application, desktop shortcuts, and configuration.",
        "path_label": "Installation directory:",
        "browse_btn": "Browse...",
        "browse_dialog": "Select destination folder for Rewind:",
        "opt_label": "Shortcuts & Startup Options:",
        "cb_desktop": "Create Desktop shortcut",
        "cb_menu": "Add to Application Menu (Dash / Start)",
        "cb_autostart": "Start with Linux (in background)",
        "cb_launch": "Launch Rewind when installation completes",
        "uninst_confirm": "Are you sure you want to uninstall Rewind?\n\nAll desktop shortcuts, menu entries, and application binaries will be removed. Your saved video clips will remain untouched.",
        "btn_install": "Install Rewind",
        "btn_uninst": "Uninstall Rewind",
        "btn_finish": "Finish",
        "btn_close": "Close",
        "prog_title_install": "Installing Rewind...",
        "prog_title_uninst": "Uninstalling Rewind...",
        "prog_closing": "Closing running instances...",
        "prog_dir": "Creating destination directories...",
        "prog_copy": "Extracting and installing application files...",
        "prog_shortcuts": "Creating Desktop and Menu shortcuts...",
        "prog_register": "Registering with Linux desktop environment...",
        "prog_done_install": "Installation completed successfully!",
        "prog_done_uninst": "Uninstallation complete!",
        "finish_install_title": "Rewind has been successfully installed!",
        "finish_install_sub": "Default clipping hotkey: [F8]. Running silently in background.",
        "finish_uninst_title": "Rewind was successfully uninstalled",
        "finish_uninst_sub": "All program files and shortcuts have been removed.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    },
    "de": {
        "name": "Deutsch", "flag": "🇩🇪",
        "lang_label": "Anwendungssprache:",
        "title_setup": "Rewind Setup v0.2",
        "title_uninst": "Rewind deinstallieren",
        "header_install_title": "Rewind Installation",
        "header_install_sub": "Blitzschnelle Gaming-Wiederholungen und Clips (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Rewind deinstallieren",
        "header_uninst_sub": "Entfernt Programm, Verknüpfungen und Konfiguration.",
        "path_label": "Installationsverzeichnis:",
        "browse_btn": "Durchsuchen...",
        "browse_dialog": "Zielordner für Rewind auswählen:",
        "opt_label": "Verknüpfungen und Autostart-Optionen:",
        "cb_desktop": "Desktop-Verknüpfung erstellen",
        "cb_menu": "Zum Anwendungsmenü hinzufügen",
        "cb_autostart": "Mit Linux starten (im Hintergrund)",
        "cb_launch": "Rewind nach der Installation starten",
        "uninst_confirm": "Möchten Sie Rewind wirklich deinstallieren?\n\nAlle Verknüpfungen und Dateien werden gelöscht. Ihre aufgenommenen Videoclips bleiben erhalten.",
        "btn_install": "Rewind installieren",
        "btn_uninst": "Programm deinstallieren",
        "btn_finish": "Fertigstellen",
        "btn_close": "Schließen",
        "prog_title_install": "Rewind wird installiert...",
        "prog_title_uninst": "Rewind wird deinstalliert...",
        "prog_closing": "Laufende Instanzen werden beendet...",
        "prog_dir": "Zielordner wird erstellt...",
        "prog_copy": "Programmdateien werden kopiert...",
        "prog_shortcuts": "Verknüpfungen werden erstellt...",
        "prog_register": "Im System registrieren...",
        "prog_done_install": "Installation erfolgreich abgeschlossen!",
        "prog_done_uninst": "Deinstallation abgeschlossen!",
        "finish_install_title": "Rewind wurde erfolgreich installiert!",
        "finish_install_sub": "Standard-Tastenkombination: [F8]. Läuft leise im Hintergrund.",
        "finish_uninst_title": "Rewind wurde deinstalliert",
        "finish_uninst_sub": "Alle Dateien und Verknüpfungen wurden entfernt.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    },
    "es": {
        "name": "Español", "flag": "🇪🇸",
        "lang_label": "Idioma de la aplicación:",
        "title_setup": "Instalador de Rewind v0.2",
        "title_uninst": "Desinstalar Rewind",
        "header_install_title": "Instalar Rewind",
        "header_install_sub": "Captura instantánea de repeticiones en juegos y escritorio (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Desinstalar Rewind",
        "header_uninst_sub": "Eliminar la aplicación, accesos directos y configuración.",
        "path_label": "Directorio de instalación:",
        "browse_btn": "Examinar...",
        "browse_dialog": "Seleccione carpeta de instalación para Rewind:",
        "opt_label": "Accesos directos e inicio automático:",
        "cb_desktop": "Crear acceso directo en el Escritorio",
        "cb_menu": "Añadir al Menú de Aplicaciones",
        "cb_autostart": "Iniciar con Linux (en segundo plano)",
        "cb_launch": "Iniciar Rewind al completar la instalación",
        "uninst_confirm": "¿Está seguro de que desea desinstalar Rewind?\n\nSe eliminarán todos los accesos directos y archivos. Sus clips de vídeo permanecerán intactos.",
        "btn_install": "Instalar Rewind",
        "btn_uninst": "Desinstalar Rewind",
        "btn_finish": "Finalizar",
        "btn_close": "Cerrar",
        "prog_title_install": "Instalando Rewind...",
        "prog_title_uninst": "Desinstalando Rewind...",
        "prog_closing": "Cerrando instancias activas...",
        "prog_dir": "Creando directorios...",
        "prog_copy": "Copiando archivos...",
        "prog_shortcuts": "Creando accesos directos...",
        "prog_register": "Registrando en el sistema...",
        "prog_done_install": "¡Instalación completada con éxito!",
        "prog_done_uninst": "¡Desinstalación completada!",
        "finish_install_title": "¡Rewind se ha instalado correctamente!",
        "finish_install_sub": "Atajo predeterminado para clips: [F8]. Ejecutándose en segundo plano.",
        "finish_uninst_title": "Rewind se ha desinstalado correctamente",
        "finish_uninst_sub": "Se han eliminado todos los archivos y accesos directos.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    },
    "fr": {
        "name": "Français", "flag": "🇫🇷",
        "lang_label": "Langue de l'application :",
        "title_setup": "Programme d'installation Rewind v0.2",
        "title_uninst": "Désinstallation de Rewind",
        "header_install_title": "Installer Rewind",
        "header_install_sub": "Capture instantanée de replays de jeux et du bureau (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Désinstaller Rewind",
        "header_uninst_sub": "Supprimer l'application, les raccourcis et la configuration.",
        "path_label": "Dossier d'installation :",
        "browse_btn": "Parcourir...",
        "browse_dialog": "Sélectionnez le dossier de destination pour Rewind :",
        "opt_label": "Raccourcis et démarrage automatique :",
        "cb_desktop": "Créer un raccourci sur le Bureau",
        "cb_menu": "Ajouter au Menu des Applications",
        "cb_autostart": "Lancer avec Linux (en arrière-plan)",
        "cb_launch": "Lancer Rewind à la fin de l'installation",
        "uninst_confirm": "Êtes-vous sûr de vouloir désinstaller Rewind ?\n\nTous les raccourcis et fichiers seront supprimés. Vos clips vidéo resteront intacts.",
        "btn_install": "Installer Rewind",
        "btn_uninst": "Désinstaller le programme",
        "btn_finish": "Terminer",
        "btn_close": "Fermer",
        "prog_title_install": "Installation de Rewind...",
        "prog_title_uninst": "Désinstallation de Rewind...",
        "prog_closing": "Fermeture des instances en cours...",
        "prog_dir": "Création des dossiers...",
        "prog_copy": "Copie des fichiers...",
        "prog_shortcuts": "Création des raccourcis...",
        "prog_register": "Enregistrement dans le système...",
        "prog_done_install": "Installation réussie !",
        "prog_done_uninst": "Désinstallation terminée !",
        "finish_install_title": "Rewind a été installé avec succès !",
        "finish_install_sub": "Raccourci par défaut pour les clips : [F8]. Fonctionne en arrière-plan.",
        "finish_uninst_title": "Rewind a été désinstallé avec succès",
        "finish_uninst_sub": "Tous les fichiers et raccourcis ont été supprimés.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    },
    "ua": {
        "name": "Українська", "flag": "🇺🇦",
        "lang_label": "Мова програми:",
        "title_setup": "Встановлення Rewind v0.2",
        "title_uninst": "Видалення Rewind",
        "header_install_title": "Встановлення Rewind",
        "header_install_sub": "Миттєвий запис ігрових повторів та кліпів (NVIDIA / VAAPI / PipeWire).",
        "header_uninst_title": "Видалити Rewind",
        "header_uninst_sub": "Видалення програми, ярликів та конфігурації.",
        "path_label": "Папка для встановлення:",
        "browse_btn": "Огляд...",
        "browse_dialog": "Оберіть папку для встановлення Rewind:",
        "opt_label": "Параметри ярликів та автозапуску:",
        "cb_desktop": "Створити ярлик на Робочому столі",
        "cb_menu": "Додати до меню «Програми»",
        "cb_autostart": "Запускати разом з Linux (у фоні)",
        "cb_launch": "Запустити Rewind після завершення встановлення",
        "uninst_confirm": "Ви дійсно бажаєте видалити Rewind?\n\nВсі ярлики та файли програми будуть видалені з комп'ютера. Ваші записані відеокліпи залишаться недоторканими.",
        "btn_install": "Встановити Rewind",
        "btn_uninst": "Видалити програму",
        "btn_finish": "Завершити",
        "btn_close": "Закрити",
        "prog_title_install": "Встановлення програми...",
        "prog_title_uninst": "Видалення програми...",
        "prog_closing": "Закриття активних процесів Rewind...",
        "prog_dir": "Створення цільових папок...",
        "prog_copy": "Копіювання файлів...",
        "prog_shortcuts": "Створення ярликів...",
        "prog_register": "Реєстрація в системі Linux...",
        "prog_done_install": "Встановлення успішно завершено!",
        "prog_done_uninst": "Видалення завершено!",
        "finish_install_title": "Rewind успішно встановлено!",
        "finish_install_sub": "Гаряча клавіша для кліпу: [F8]. Програма працює у фоні.",
        "finish_uninst_title": "Rewind успішно видалено",
        "finish_uninst_sub": "Всі файли та ярлики програми видалено.",
        "footer_hint": "Rewind v0.2 Setup (Linux x86_64)",
    }
}


def detect_system_language():
    try:
        lang = os.environ.get("LANG", "pl").split("_")[0].lower()
        if lang in LANGUAGES:
            return lang
    except Exception:
        pass
    return "pl"


def extract_payload(dest_file):
    """Extract embedded application payload to target path."""
    dest_path = Path(dest_file)
    dest_path.parent.mkdir(parents=True, exist_ok=True)

    if PAYLOAD_GZ_B64:
        raw_bin = gzip.decompress(base64.b64decode(PAYLOAD_GZ_B64))
        dest_path.write_bytes(raw_bin)
    else:
        # Fallback to local files if developing unbundled
        script_dir = Path(__file__).resolve().parent
        for cand in ["rewind-linux", "rewind.py", "rewind"]:
            f = script_dir / cand
            if f.exists():
                shutil.copy2(str(f), str(dest_path))
                break
        else:
            dest_path.write_text("#!/usr/bin/env python3\n")

    dest_path.chmod(0o755)
    return dest_path


def install_rewind_core(target_dir_str, create_desktop, create_menu, create_autostart, lang_code="pl"):
    """Core installation logic used by both GUI and CLI."""
    target_dir = Path(target_dir_str)
    home = Path.home()

    if os.geteuid() == 0:
        bin_dir = Path("/usr/local/bin")
        apps_dir = Path("/usr/share/applications")
        icons_dir = Path("/usr/share/icons/hicolor/256x256/apps")
        sudo_user = os.environ.get("SUDO_USER")
        if sudo_user:
            user_home = Path(f"/home/{sudo_user}")
            desktop_dir = user_home / "Desktop"
            autostart_dir = user_home / ".config" / "autostart"
        else:
            desktop_dir = home / "Desktop"
            autostart_dir = home / ".config" / "autostart"
    else:
        bin_dir = home / ".local" / "bin"
        apps_dir = home / ".local" / "share" / "applications"
        icons_dir = home / ".local" / "share" / "icons" / "hicolor" / "256x256" / "apps"
        desktop_dir = home / "Desktop"
        autostart_dir = home / ".config" / "autostart"

    # Stop running instances
    try:
        subprocess.run(["pkill", "-f", "rewind"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception:
        pass

    # Create directories
    target_dir.mkdir(parents=True, exist_ok=True)
    bin_dir.mkdir(parents=True, exist_ok=True)
    apps_dir.mkdir(parents=True, exist_ok=True)
    icons_dir.mkdir(parents=True, exist_ok=True)
    (home / "Videos" / "Rewind").mkdir(parents=True, exist_ok=True)
    (home / ".config" / "rewind").mkdir(parents=True, exist_ok=True)

    # Extract application binary into target directory
    dest_bin = target_dir / "rewind"
    extract_payload(dest_bin)

    # Create symlink or wrapper in bin directory
    sym_bin = bin_dir / "rewind"
    try:
        if sym_bin.exists() or sym_bin.is_symlink():
            sym_bin.unlink()
        sym_bin.symlink_to(dest_bin)
    except Exception:
        try:
            shutil.copy2(str(dest_bin), str(sym_bin))
            sym_bin.chmod(0o755)
        except Exception:
            pass

    installed_bin = sym_bin if sym_bin.exists() else dest_bin

    # Extract icon
    dest_icon = icons_dir / "rewind.png"
    cfg_icon = home / ".config" / "rewind" / "icon.png"
    if LOGO_PNG_B64:
        raw_icon = base64.b64decode(LOGO_PNG_B64)
        dest_icon.write_bytes(raw_icon)
        cfg_icon.write_bytes(raw_icon)

    # Desktop shortcut template with quoted binary paths
    desktop_content = f"""[Desktop Entry]
Name=Rewind
GenericName=Replay Recorder
Comment=Instant replay rolling buffer clip recorder
Exec="{installed_bin}"
Icon=rewind
Terminal=false
Type=Application
Categories=AudioVideo;Recorder;Utility;
Keywords=replay;clip;recording;gameplay;shadowplay;
StartupNotify=true
Actions=SaveClip;ToggleBuffer;

[Desktop Action SaveClip]
Name=Zapisz powtórkę
Exec="{installed_bin}" --save

[Desktop Action ToggleBuffer]
Name=Wstrzymaj / Wznów bufor
Exec="{installed_bin}" --toggle
"""

    # 1. Application Menu (.desktop)
    if create_menu:
        app_desktop = apps_dir / "rewind.desktop"
        app_desktop.write_text(desktop_content, encoding="utf-8")
        app_desktop.chmod(0o755)

    # 2. Desktop Shortcut (.desktop)
    if create_desktop:
        desktop_dir.mkdir(parents=True, exist_ok=True)
        desk_file = desktop_dir / "rewind.desktop"
        desk_file.write_text(desktop_content, encoding="utf-8")
        desk_file.chmod(0o755)
        try:
            subprocess.run(["gio", "set", str(desk_file), "metadata::trusted", "true"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        except Exception:
            pass

    # 3. Autostart Shortcut
    if create_autostart:
        autostart_dir.mkdir(parents=True, exist_ok=True)
        auto_file = autostart_dir / "rewind.desktop"
        auto_desktop = desktop_content.replace(f'Exec="{installed_bin}"', f'Exec="{installed_bin}" --daemon')
        auto_file.write_text(auto_desktop, encoding="utf-8")
        auto_file.chmod(0o755)

    # Update desktop database
    try:
        subprocess.run(["update-desktop-database", str(apps_dir)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception:
        pass

    # Save initial settings with language
    try:
        settings_json = home / ".config" / "rewind" / "rewind_settings.json"
        if not settings_json.exists():
            init_conf = f'{{"Language": "{lang_code}", "AutoStart": {str(create_autostart).lower()}}}\n'
            settings_json.write_text(init_conf, encoding="utf-8")
    except Exception:
        pass

    return installed_bin


def uninstall_rewind_core():
    """Core uninstallation logic (W-02)."""
    home = Path.home()
    if os.geteuid() == 0:
        target_dir = Path("/usr/local/share/rewind")
        bin_file = Path("/usr/local/bin/rewind")
        apps_file = Path("/usr/share/applications/rewind.desktop")
        icon_file = Path("/usr/share/icons/hicolor/256x256/apps/rewind.png")
        sudo_user = os.environ.get("SUDO_USER")
        if sudo_user:
            user_desk = Path(f"/home/{sudo_user}/Desktop/rewind.desktop")
            user_auto = Path(f"/home/{sudo_user}/.config/autostart/rewind.desktop")
        else:
            user_desk = home / "Desktop/rewind.desktop"
            user_auto = home / ".config/autostart/rewind.desktop"
    else:
        target_dir = home / ".local/share/rewind"
        bin_file = home / ".local/bin/rewind"
        apps_file = home / ".local/share/applications/rewind.desktop"
        icon_file = home / ".local/share/icons/hicolor/256x256/apps/rewind.png"
        user_desk = home / "Desktop/rewind.desktop"
        user_auto = home / ".config/autostart/rewind.desktop"

    try:
        subprocess.run(["pkill", "-f", "([r]ewind|[r]ewind-linux|[R]ewindSetup)"], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception:
        pass

    # Clean runtime sockets & locks
    runtime_dir = os.environ.get("XDG_RUNTIME_DIR")
    if runtime_dir:
        for f in Path(runtime_dir).glob("rewind*"):
            try:
                if f.is_file() or f.is_socket():
                    f.unlink()
                elif f.is_dir() and "rewind" in f.name:
                    shutil.rmtree(str(f), ignore_errors=True)
            except Exception:
                pass
    for f in Path(tempfile.gettempdir()).glob("rewind*"):
        try:
            if f.is_file() or f.is_socket():
                f.unlink()
            elif f.is_dir() and "rewind" in f.name:
                shutil.rmtree(str(f), ignore_errors=True)
        except Exception:
            pass

    for f in [bin_file, apps_file, icon_file, user_desk, user_auto]:
        try:
            if f.exists() or f.is_symlink():
                f.unlink()
        except Exception:
            pass

    try:
        if target_dir.exists():
            shutil.rmtree(str(target_dir), ignore_errors=True)
    except Exception:
        pass

    try:
        subprocess.run(["update-desktop-database", str(apps_file.parent)], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except Exception:
        pass


class RewindInstallerGui:
    def __init__(self, is_uninstall=False):
        import tkinter as tk
        from tkinter import filedialog, messagebox

        self.tk = tk
        self.filedialog = filedialog
        self.messagebox = messagebox

        self.is_uninstall = is_uninstall
        self.current_lang = detect_system_language()
        self.s = LANGUAGES[self.current_lang]
        self.is_completed = False

        home = Path.home()
        if os.geteuid() == 0:
            self.default_target = "/usr/local/share/rewind"
        else:
            self.default_target = str(home / ".local" / "share" / "rewind")

        self.target_dir_var = None
        self.installed_bin = None

        # Dark Modern Palette (Matching Windows Setup)
        self.C_BG = "#0D0D11"
        self.C_HEADER = "#121218"
        self.C_CARD = "#181822"
        self.C_BORDER = "#282838"
        self.C_TEXT = "#F3F3F6"
        self.C_MUTED = "#8E8E9E"
        self.C_ACCENT = "#FF5757"
        self.C_ACCENT_HOVER = "#FF6E6E"
        self.C_GREEN = "#10B981"
        self.C_INPUT_BG = "#1F1F2C"
        self.C_INPUT_BORDER = "#2E2E40"
        self.C_PILL_BG = "#22222E"
        self.C_PILL_BORDER = "#353548"
        self.C_PILL_ACTIVE_BG = "#381E29"
        self.C_PILL_ACTIVE_BORDER = "#FF5757"
        self.C_PILL_ACTIVE_FG = "#FF8A8A"

        self.root = tk.Tk()
        self.root.title(self.s["title_uninst"] if is_uninstall else self.s["title_setup"])
        self.root.geometry("580x560")
        self.root.minsize(560, 540)
        self.root.configure(bg=self.C_BG)

        self.icon_photo = None
        if LOGO_PNG_B64:
            try:
                raw_data = base64.b64decode(LOGO_PNG_B64)
                self.icon_photo = tk.PhotoImage(data=raw_data)
                self.root.iconphoto(True, self.icon_photo)
            except Exception:
                pass

        self.pill_buttons = {}
        self.build_ui()
        self.refresh_language()

    def build_ui(self):
        tk = self.tk

        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=0)  # Header
        self.root.rowconfigure(1, weight=1)  # Content view
        self.root.rowconfigure(2, weight=0)  # Footer action

        # 1. Brand Header
        self.header_frame = tk.Frame(self.root, bg=self.C_HEADER, height=84)
        self.header_frame.grid(row=0, column=0, sticky="ew")
        self.header_frame.grid_propagate(False)

        h_inner = tk.Frame(self.header_frame, bg=self.C_HEADER)
        h_inner.pack(fill="both", expand=True, padx=24, pady=16)

        if self.icon_photo:
            try:
                self.logo_lbl = tk.Label(h_inner, image=self.icon_photo, bg=self.C_HEADER)
                self.logo_lbl.pack(side="left", padx=(0, 14))
            except Exception:
                pass

        h_text = tk.Frame(h_inner, bg=self.C_HEADER)
        h_text.pack(side="left", fill="both", expand=True)

        self.h_title = tk.Label(
            h_text,
            text=self.s["header_uninst_title"] if self.is_uninstall else self.s["header_install_title"],
            font=("Segoe UI", 15, "bold"),
            fg=self.C_TEXT,
            bg=self.C_HEADER
        )
        self.h_title.pack(anchor="w")

        self.h_sub = tk.Label(
            h_text,
            text=self.s["header_uninst_sub"] if self.is_uninstall else self.s["header_install_sub"],
            font=("Segoe UI", 9),
            fg=self.C_MUTED,
            bg=self.C_HEADER
        )
        self.h_sub.pack(anchor="w", pady=(2, 0))

        # 2. Main Content Container
        self.content_container = tk.Frame(self.root, bg=self.C_BG)
        self.content_container.grid(row=1, column=0, sticky="nsew", padx=24, pady=14)
        self.content_container.columnconfigure(0, weight=1)
        self.content_container.rowconfigure(0, weight=1)

        # A. View Config
        self.view_config = tk.Frame(self.content_container, bg=self.C_BG)
        self.view_config.grid(row=0, column=0, sticky="nsew")

        if not self.is_uninstall:
            # Language card
            card_lang = tk.Frame(self.view_config, bg=self.C_CARD, bd=1, relief="solid", highlightbackground=self.C_BORDER, highlightthickness=1)
            card_lang.pack(fill="x", pady=(0, 10))

            c_lang_in = tk.Frame(card_lang, bg=self.C_CARD)
            c_lang_in.pack(fill="x", padx=16, pady=12)

            self.lang_lbl = tk.Label(c_lang_in, text=self.s["lang_label"], font=("Segoe UI", 10, "bold"), fg=self.C_TEXT, bg=self.C_CARD)
            self.lang_lbl.pack(anchor="w", pady=(0, 8))

            pills_row = tk.Frame(c_lang_in, bg=self.C_CARD)
            pills_row.pack(anchor="w")

            for code, data in LANGUAGES.items():
                lbl_text = f"{data['flag']} {data['name']}"
                btn = tk.Button(
                    pills_row,
                    text=lbl_text,
                    font=("Segoe UI", 9),
                    bd=1,
                    relief="solid",
                    padx=10,
                    pady=4,
                    cursor="hand2",
                    command=lambda c=code: self.switch_language(c)
                )
                btn.pack(side="left", padx=3)
                self.pill_buttons[code] = btn

            # Path card
            card_path = tk.Frame(self.view_config, bg=self.C_CARD, bd=1, relief="solid", highlightbackground=self.C_BORDER, highlightthickness=1)
            card_path.pack(fill="x", pady=(0, 10))

            c_path_in = tk.Frame(card_path, bg=self.C_CARD)
            c_path_in.pack(fill="x", padx=16, pady=12)

            self.path_lbl = tk.Label(c_path_in, text=self.s["path_label"], font=("Segoe UI", 10, "bold"), fg=self.C_TEXT, bg=self.C_CARD)
            self.path_lbl.pack(anchor="w", pady=(0, 6))

            path_input_row = tk.Frame(c_path_in, bg=self.C_CARD)
            path_input_row.pack(fill="x")

            self.target_dir_var = tk.StringVar(value=self.default_target)
            self.path_entry = tk.Entry(
                path_input_row,
                textvariable=self.target_dir_var,
                font=("Segoe UI", 10),
                bg=self.C_INPUT_BG,
                fg=self.C_TEXT,
                bd=1,
                relief="solid",
                highlightbackground=self.C_INPUT_BORDER,
                highlightthickness=1
            )
            self.path_entry.pack(side="left", fill="x", expand=True, ipady=4)

            self.browse_btn = tk.Button(
                path_input_row,
                text=self.s["browse_btn"],
                font=("Segoe UI", 9, "bold"),
                bg=self.C_PILL_BG,
                fg=self.C_TEXT,
                bd=1,
                relief="solid",
                highlightbackground=self.C_PILL_BORDER,
                highlightthickness=1,
                padx=12,
                pady=4,
                cursor="hand2",
                command=self.browse_directory
            )
            self.browse_btn.pack(side="left", padx=(8, 0))

            # Options card
            card_opt = tk.Frame(self.view_config, bg=self.C_CARD, bd=1, relief="solid", highlightbackground=self.C_BORDER, highlightthickness=1)
            card_opt.pack(fill="x")

            c_opt_in = tk.Frame(card_opt, bg=self.C_CARD)
            c_opt_in.pack(fill="x", padx=16, pady=12)

            self.opt_lbl = tk.Label(c_opt_in, text=self.s["opt_label"], font=("Segoe UI", 10, "bold"), fg=self.C_TEXT, bg=self.C_CARD)
            self.opt_lbl.pack(anchor="w", pady=(0, 8))

            self.var_desktop = tk.BooleanVar(value=True)
            self.var_menu = tk.BooleanVar(value=True)
            self.var_autostart = tk.BooleanVar(value=False)
            self.var_launch = tk.BooleanVar(value=True)

            self.cb_desktop = self.create_checkbox(c_opt_in, self.s["cb_desktop"], self.var_desktop)
            self.cb_menu = self.create_checkbox(c_opt_in, self.s["cb_menu"], self.var_menu)
            self.cb_autostart = self.create_checkbox(c_opt_in, self.s["cb_autostart"], self.var_autostart)
            self.cb_launch = self.create_checkbox(c_opt_in, self.s["cb_launch"], self.var_launch)
        else:
            card_uninst = tk.Frame(self.view_config, bg=self.C_CARD, bd=1, relief="solid", highlightbackground=self.C_BORDER, highlightthickness=1)
            card_uninst.pack(fill="both", expand=True)

            c_uninst_in = tk.Frame(card_uninst, bg=self.C_CARD)
            c_uninst_in.pack(fill="both", expand=True, padx=20, pady=20)

            self.uninst_txt = tk.Label(
                c_uninst_in,
                text=self.s["uninst_confirm"],
                font=("Segoe UI", 11),
                fg=self.C_TEXT,
                bg=self.C_CARD,
                justify="left",
                wraplength=490
            )
            self.uninst_txt.pack(anchor="w")

        # B. View Progress
        self.view_progress = tk.Frame(self.content_container, bg=self.C_BG)

        prog_inner = tk.Frame(self.view_progress, bg=self.C_BG)
        prog_inner.pack(expand=True, fill="both", pady=40)

        self.prog_title_lbl = tk.Label(
            prog_inner,
            text=self.s["prog_title_uninst"] if self.is_uninstall else self.s["prog_title_install"],
            font=("Segoe UI", 15, "bold"),
            fg=self.C_TEXT,
            bg=self.C_BG
        )
        self.prog_title_lbl.pack(pady=(0, 6))

        self.prog_sub_lbl = tk.Label(
            prog_inner,
            text=self.s["prog_copy"],
            font=("Segoe UI", 10),
            fg=self.C_MUTED,
            bg=self.C_BG
        )
        self.prog_sub_lbl.pack(pady=(0, 24))

        self.progress_canvas = tk.Canvas(prog_inner, width=480, height=10, bg=self.C_CARD, bd=0, highlightthickness=1, highlightbackground=self.C_BORDER)
        self.progress_canvas.pack()

        # C. View Finish
        self.view_finish = tk.Frame(self.content_container, bg=self.C_BG)

        finish_inner = tk.Frame(self.view_finish, bg=self.C_BG)
        finish_inner.pack(expand=True, fill="both", pady=40)

        self.check_badge = tk.Label(
            finish_inner,
            text="✓",
            font=("Segoe UI", 28, "bold"),
            fg=self.C_GREEN,
            bg=self.C_BG
        )
        self.check_badge.pack(pady=(0, 10))

        self.finish_title_lbl = tk.Label(
            finish_inner,
            text=self.s["finish_uninst_title"] if self.is_uninstall else self.s["finish_install_title"],
            font=("Segoe UI", 16, "bold"),
            fg=self.C_TEXT,
            bg=self.C_BG
        )
        self.finish_title_lbl.pack(pady=(0, 6))

        self.finish_sub_lbl = tk.Label(
            finish_inner,
            text=self.s["finish_uninst_sub"] if self.is_uninstall else self.s["finish_install_sub"],
            font=("Segoe UI", 10),
            fg=self.C_MUTED,
            bg=self.C_BG,
            justify="center",
            wraplength=480
        )
        self.finish_sub_lbl.pack()

        # 3. Footer Action Bar
        self.footer = tk.Frame(self.root, bg=self.C_HEADER, height=68, bd=1, relief="solid", highlightbackground=self.C_BORDER, highlightthickness=1)
        self.footer.grid(row=2, column=0, sticky="ew")
        self.footer.grid_propagate(False)

        foot_inner = tk.Frame(self.footer, bg=self.C_HEADER)
        foot_inner.pack(fill="both", expand=True, padx=24, pady=14)

        self.footer_lbl = tk.Label(
            foot_inner,
            text=self.s["footer_hint"],
            font=("Segoe UI", 9),
            fg=self.C_MUTED,
            bg=self.C_HEADER
        )
        self.footer_lbl.pack(side="left")

        self.action_btn = tk.Button(
            foot_inner,
            text=self.s["btn_uninst"] if self.is_uninstall else self.s["btn_install"],
            font=("Segoe UI", 10, "bold"),
            bg=self.C_ACCENT,
            fg="#FFFFFF",
            bd=0,
            padx=20,
            pady=6,
            cursor="hand2",
            command=self.on_action_click
        )
        self.action_btn.pack(side="right")
        self.action_btn.bind("<Enter>", lambda e: self.action_btn.configure(bg=self.C_ACCENT_HOVER))
        self.action_btn.bind("<Leave>", lambda e: self.action_btn.configure(bg=self.C_ACCENT))

    def create_checkbox(self, parent, text, var):
        tk = self.tk
        cb = tk.Checkbutton(
            parent,
            text=text,
            variable=var,
            font=("Segoe UI", 9.5),
            fg=self.C_TEXT,
            bg=self.C_CARD,
            activebackground=self.C_CARD,
            activeforeground=self.C_TEXT,
            selectcolor=self.C_INPUT_BG,
            bd=0,
            highlightthickness=0,
            cursor="hand2"
        )
        cb.pack(anchor="w", pady=2)
        return cb

    def switch_language(self, code):
        if code in LANGUAGES:
            self.current_lang = code
            self.s = LANGUAGES[code]
            self.refresh_language()

    def refresh_language(self):
        s = self.s
        self.root.title(s["title_uninst"] if self.is_uninstall else s["title_setup"])
        self.h_title.configure(text=s["header_uninst_title"] if self.is_uninstall else s["header_install_title"])
        self.h_sub.configure(text=s["header_uninst_sub"] if self.is_uninstall else s["header_install_sub"])

        for c, btn in self.pill_buttons.items():
            if c == self.current_lang:
                btn.configure(bg=self.C_PILL_ACTIVE_BG, fg=self.C_PILL_ACTIVE_FG, highlightbackground=self.C_PILL_ACTIVE_BORDER)
            else:
                btn.configure(bg=self.C_PILL_BG, fg=self.C_MUTED, highlightbackground=self.C_PILL_BORDER)

        if not self.is_uninstall:
            self.lang_lbl.configure(text=s["lang_label"])
            self.path_lbl.configure(text=s["path_label"])
            self.browse_btn.configure(text=s["browse_btn"])
            self.opt_lbl.configure(text=s["opt_label"])
            self.cb_desktop.configure(text=s["cb_desktop"])
            self.cb_menu.configure(text=s["cb_menu"])
            self.cb_autostart.configure(text=s["cb_autostart"])
            self.cb_launch.configure(text=s["cb_launch"])
        else:
            self.uninst_txt.configure(text=s["uninst_confirm"])

        self.prog_title_lbl.configure(text=s["prog_title_uninst"] if self.is_uninstall else s["prog_title_install"])
        self.finish_title_lbl.configure(text=s["finish_uninst_title"] if self.is_uninstall else s["finish_install_title"])
        self.finish_sub_lbl.configure(text=s["finish_uninst_sub"] if self.is_uninstall else s["finish_install_sub"])

        if self.is_completed:
            self.action_btn.configure(text=s["btn_close"] if self.is_uninstall else s["btn_finish"])
        else:
            self.action_btn.configure(text=s["btn_uninst"] if self.is_uninstall else s["btn_install"])

    def browse_directory(self):
        chosen = self.filedialog.askdirectory(title=self.s["browse_dialog"], initialdir=self.target_dir_var.get())
        if chosen:
            p = Path(chosen)
            if p.name.lower() != "rewind":
                p = p / "rewind"
            self.target_dir_var.set(str(p))

    def update_progress(self, percent, subtitle):
        self.prog_sub_lbl.configure(text=subtitle)
        canvas_width = 480
        bar_w = int(canvas_width * (percent / 100.0))
        self.progress_canvas.delete("all")
        if bar_w > 0:
            self.progress_canvas.create_rectangle(0, 0, bar_w, 10, fill=self.C_ACCENT, width=0)
        self.root.update_idletasks()

    def on_action_click(self):
        if self.is_completed:
            if not self.is_uninstall and self.var_launch.get() and self.installed_bin:
                try:
                    subprocess.Popen([str(self.installed_bin)], start_new_session=True)
                except Exception:
                    pass
            self.root.destroy()
            return

        self.view_config.grid_forget()
        self.view_progress.grid(row=0, column=0, sticky="nsew")
        self.action_btn.configure(state="disabled")

        if self.is_uninstall:
            t = threading.Thread(target=self.run_uninstall_worker)
            t.daemon = True
            t.start()
        else:
            t = threading.Thread(target=self.run_install_worker)
            t.daemon = True
            t.start()

    def run_install_worker(self):
        s = self.s
        time.sleep(0.2)
        self.update_progress(15, s["prog_closing"])
        time.sleep(0.3)
        self.update_progress(35, s["prog_dir"])
        time.sleep(0.3)
        self.update_progress(60, s["prog_copy"])

        self.installed_bin = install_rewind_core(
            target_dir_str=self.target_dir_var.get(),
            create_desktop=self.var_desktop.get(),
            create_menu=self.var_menu.get(),
            create_autostart=self.var_autostart.get(),
            lang_code=self.current_lang
        )

        time.sleep(0.3)
        self.update_progress(80, s["prog_shortcuts"])
        time.sleep(0.3)
        self.update_progress(95, s["prog_register"])
        time.sleep(0.3)
        self.update_progress(100, s["prog_done_install"])
        time.sleep(0.4)

        self.root.after(100, self.finish_installation)

    def run_uninstall_worker(self):
        s = self.s
        time.sleep(0.2)
        self.update_progress(30, s["prog_closing"])
        time.sleep(0.3)
        self.update_progress(60, s["prog_shortcuts"])

        uninstall_rewind_core()

        time.sleep(0.3)
        self.update_progress(90, s["prog_register"])
        time.sleep(0.3)
        self.update_progress(100, s["prog_done_uninst"])
        time.sleep(0.4)
        self.root.after(100, self.finish_installation)

    def finish_installation(self):
        self.is_completed = True
        self.view_progress.grid_forget()
        self.view_finish.grid(row=0, column=0, sticky="nsew")
        self.action_btn.configure(
            state="normal",
            text=self.s["btn_close"] if self.is_uninstall else self.s["btn_finish"],
            bg=self.C_GREEN
        )
        self.action_btn.bind("<Enter>", lambda e: self.action_btn.configure(bg="#34D399"))
        self.action_btn.bind("<Leave>", lambda e: self.action_btn.configure(bg=self.C_GREEN))


def run_cli_install(target_dir=None, autostart=False):
    """Clean terminal installer for headless or CLI environments."""
    CYAN = '\033[0;36m'
    GREEN = '\033[0;32m'
    NC = '\033[0m'
    print(f"{CYAN}=== Instalator Rewind v0.2 dla Linux (CLI Setup) ==={NC}")

    if not target_dir:
        home = Path.home()
        if os.geteuid() == 0:
            target_dir = "/usr/local/share/rewind"
        else:
            target_dir = str(home / ".local/share/rewind")

    print(f"Instalowanie do: {target_dir}...")
    bin_path = install_rewind_core(
        target_dir_str=target_dir,
        create_desktop=True,
        create_menu=True,
        create_autostart=autostart,
        lang_code=detect_system_language()
    )

    print(f"{GREEN}✓ Rewind został pomyślnie zainstalowany!{NC}")
    print(f"• Uruchomienie programu: kliknij ikonę Rewind na Pulpicie lub wpisz {CYAN}rewind{NC} w terminalu.")
    print(f"• Skrót do zapisu klipu: {CYAN}F8{NC} (lub polecenie {CYAN}rewind --save{NC})")


def run_cli_uninstall():
    """Terminal uninstallation."""
    CYAN = '\033[0;36m'
    GREEN = '\033[0;32m'
    NC = '\033[0m'
    print(f"{CYAN}=== Deinstalator Rewind dla Linux (CLI) ==={NC}")
    uninstall_rewind_core()
    print(f"{GREEN}✓ Rewind został odinstalowany z systemu.{NC}")


def main():
    parser = argparse.ArgumentParser(description="Rewind Linux Setup (GUI & CLI Installer)")
    parser.add_argument("--uninstall", action="store_true", help="Odinstaluj program Rewind")
    parser.add_argument("--cli", action="store_true", help="Uruchom w trybie tekstowym bez okna GUI")
    parser.add_argument("--target", type=str, default="", help="Katalog docelowy dla trybu CLI")
    parser.add_argument("--autostart", action="store_true", help="Włącz autostart w tle")
    args = parser.parse_args()

    has_display = bool(os.environ.get("DISPLAY") or os.environ.get("WAYLAND_DISPLAY"))

    if args.cli or not has_display:
        if args.uninstall:
            run_cli_uninstall()
        else:
            run_cli_install(target_dir=args.target, autostart=args.autostart)
        sys.exit(0)

    try:
        gui = RewindInstallerGui(is_uninstall=args.uninstall)
        gui.root.mainloop()
    except Exception as ex:
        print(f"[RewindSetup] Nie można uruchomić interfejsu graficznego ({ex}). Przełączanie w tryb terminalowy...", file=sys.stderr)
        if args.uninstall:
            run_cli_uninstall()
        else:
            run_cli_install(target_dir=args.target, autostart=args.autostart)


if __name__ == "__main__":
    main()
