#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Installer (GUI + CLI)
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# If GUI display is available and not explicitly forced to CLI, launch the windowed installer!
USE_CLI=0
for arg in "$@"; do
    if [ "$arg" = "--cli" ] || [ "$arg" = "-c" ] || [ "$arg" = "--no-gui" ]; then
        USE_CLI=1
        break
    fi
done

if [ "$USE_CLI" -eq 0 ] && ([ -n "$DISPLAY" ] || [ -n "$WAYLAND_DISPLAY" ]); then
    if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
        exec "$SCRIPT_DIR/RewindSetup" "$@"
    elif [ -f "$SCRIPT_DIR/installer.py" ]; then
        exec python3 "$SCRIPT_DIR/installer.py" "$@"
    fi
fi

# Fallback: CLI Installation
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${CYAN}=== Instalator Rewind v0.2 dla Linux ===${NC}"

# Check for Python 3
if ! command -v python3 &> /dev/null; then
    echo -e "${RED}[Błąd] Python 3 nie został znaleziony! Zainstaluj python3.${NC}"
    exit 1
fi

# Check for FFmpeg
if ! command -v ffmpeg &> /dev/null; then
    echo -e "${YELLOW}[Ostrzeżenie] FFmpeg nie został znaleziony. Zainstaluj ffmpeg przed użyciem (np. sudo apt install ffmpeg lub sudo pacman -S ffmpeg).${NC}"
fi

# Detect paths
if [ "$EUID" -eq 0 ]; then
    TARGET_DIR="/usr/local/share/rewind"
    BIN_DIR="/usr/local/bin"
    APPS_DIR="/usr/share/applications"
    ICONS_DIR="/usr/share/icons/hicolor/256x256/apps"
    if [ -n "$SUDO_USER" ]; then
        DESKTOP_DIR="/home/$SUDO_USER/Desktop"
    else
        DESKTOP_DIR="$HOME/Desktop"
    fi
else
    TARGET_DIR="$HOME/.local/share/rewind"
    BIN_DIR="$HOME/.local/bin"
    APPS_DIR="$HOME/.local/share/applications"
    ICONS_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"
    DESKTOP_DIR="$HOME/Desktop"
fi

mkdir -p "$TARGET_DIR"
mkdir -p "$BIN_DIR"
mkdir -p "$APPS_DIR"
mkdir -p "$ICONS_DIR"
mkdir -p "$HOME/Videos/Rewind"
mkdir -p "$HOME/.config/rewind"

# Copy binary
echo -e "${CYAN}Kopiowanie plików programu Rewind...${NC}"
INSTALLED_BIN="$TARGET_DIR/rewind"
if [ -f "$SCRIPT_DIR/rewind-linux" ]; then
    cp "$SCRIPT_DIR/rewind-linux" "$INSTALLED_BIN"
elif [ -f "$SCRIPT_DIR/rewind.py" ]; then
    cp "$SCRIPT_DIR/rewind.py" "$INSTALLED_BIN"
fi
chmod +x "$INSTALLED_BIN"

# Create symlink in bin directory
ln -sf "$INSTALLED_BIN" "$BIN_DIR/rewind"

# Copy icon
if [ -f "$SCRIPT_DIR/icon.png" ]; then
    cp "$SCRIPT_DIR/icon.png" "$ICONS_DIR/rewind.png"
    cp "$SCRIPT_DIR/icon.png" "$HOME/.config/rewind/icon.png"
fi

# Install desktop entry
echo -e "${CYAN}Rejestrowanie skrótów w Menu Aplikacji oraz na Pulpicie...${NC}"
cat <<EOF > "$APPS_DIR/rewind.desktop"
[Desktop Entry]
Name=Rewind
GenericName=Replay Recorder
Comment=Instant replay rolling buffer clip recorder
Exec=$BIN_DIR/rewind
Icon=rewind
Terminal=false
Type=Application
Categories=AudioVideo;Recorder;Utility;
Keywords=replay;clip;recording;gameplay;shadowplay;
StartupNotify=true
Actions=SaveClip;ToggleBuffer;

[Desktop Action SaveClip]
Name=Zapisz powtórkę
Exec=$BIN_DIR/rewind --save

[Desktop Action ToggleBuffer]
Name=Wstrzymaj / Wznów bufor
Exec=$BIN_DIR/rewind --toggle
EOF

chmod +x "$APPS_DIR/rewind.desktop"

# Create desktop shortcut if Desktop exists
if [ -d "$DESKTOP_DIR" ]; then
    cp "$APPS_DIR/rewind.desktop" "$DESKTOP_DIR/rewind.desktop"
    chmod +x "$DESKTOP_DIR/rewind.desktop"
    if command -v gio &> /dev/null; then
        gio set "$DESKTOP_DIR/rewind.desktop" metadata::trusted true 2>/dev/null || true
    fi
fi

if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$APPS_DIR" 2>/dev/null || true
fi

echo -e "${GREEN}✓ Rewind został pomyślnie zainstalowany!${NC}"
echo -e "• Uruchomienie programu: kliknij ikonę Rewind na Pulpicie, w Menu Aplikacji lub wpisz ${CYAN}rewind${NC} w terminalu."
echo -e "• Skrót do zapisu klipu: ${CYAN}F8${NC} (lub polecenie ${CYAN}rewind --save${NC})"
echo -e "• Katalog klipów: ${CYAN}$HOME/Videos/Rewind/${NC}"
