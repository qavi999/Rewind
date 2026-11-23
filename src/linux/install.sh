#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Installer
# =============================================================================

set -e

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
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "$EUID" -eq 0 ]; then
    BIN_DIR="/usr/local/bin"
    APPS_DIR="/usr/share/applications"
    ICONS_DIR="/usr/share/icons/hicolor/256x256/apps"
else
    BIN_DIR="$HOME/.local/bin"
    APPS_DIR="$HOME/.local/share/applications"
    ICONS_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"
fi

mkdir -p "$BIN_DIR"
mkdir -p "$APPS_DIR"
mkdir -p "$ICONS_DIR"
mkdir -p "$HOME/Videos/Rewind"
mkdir -p "$HOME/.config/rewind"

# Copy binary
echo -e "${CYAN}Kopiowanie pliku wykonywalnego do $BIN_DIR/rewind...${NC}"
if [ -f "$SCRIPT_DIR/rewind-linux" ]; then
    cp "$SCRIPT_DIR/rewind-linux" "$BIN_DIR/rewind"
elif [ -f "$SCRIPT_DIR/rewind.py" ]; then
    cp "$SCRIPT_DIR/rewind.py" "$BIN_DIR/rewind"
fi
chmod +x "$BIN_DIR/rewind"

# Copy icon
if [ -f "$SCRIPT_DIR/icon.png" ]; then
    cp "$SCRIPT_DIR/icon.png" "$ICONS_DIR/rewind.png"
    cp "$SCRIPT_DIR/icon.png" "$HOME/.config/rewind/icon.png"
fi

# Install desktop entry
echo -e "${CYAN}Rejestrowanie skrótu w menu aplikacji ($APPS_DIR/rewind.desktop)...${NC}"
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

if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$APPS_DIR" 2>/dev/null || true
fi

echo -e "${GREEN}✓ Rewind został pomyślnie zainstalowany!${NC}"
echo -e "• Uruchomienie programu: wpisz ${CYAN}rewind${NC} lub wybierz z menu aplikacji."
echo -e "• Natychmiastowy zapis powtórki: ${CYAN}rewind --save${NC}"
echo -e "• Katalog klipów: ${CYAN}$HOME/Videos/Rewind/${NC}"
echo ""
echo -e "${YELLOW}Wskazówka Wayland:${NC} Przypisz klawisz (np. F8) w Ustawieniach Systemowych -> Skróty Klawiszowe do polecenia: ${CYAN}rewind --save${NC}"
