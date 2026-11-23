#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Uninstaller (GUI + CLI)
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

USE_CLI=0
for arg in "$@"; do
    if [ "$arg" = "--cli" ] || [ "$arg" = "-c" ] || [ "$arg" = "--no-gui" ]; then
        USE_CLI=1
        break
    fi
done

if [ "$USE_CLI" -eq 0 ] && ([ -n "$DISPLAY" ] || [ -n "$WAYLAND_DISPLAY" ]); then
    if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
        exec "$SCRIPT_DIR/RewindSetup" --uninstall "$@"
    elif [ -f "$SCRIPT_DIR/installer.py" ]; then
        exec python3 "$SCRIPT_DIR/installer.py" --uninstall "$@"
    fi
fi

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}=== Deinstalator Rewind dla Linux ===${NC}"

if [ "$EUID" -eq 0 ]; then
    TARGET_DIR="/usr/local/share/rewind"
    BIN_FILE="/usr/local/bin/rewind"
    DESKTOP_FILE="/usr/share/applications/rewind.desktop"
    ICON_FILE="/usr/share/icons/hicolor/256x256/apps/rewind.png"
    if [ -n "$SUDO_USER" ]; then
        USER_DESK="/home/$SUDO_USER/Desktop/rewind.desktop"
    else
        USER_DESK="$HOME/Desktop/rewind.desktop"
    fi
else
    TARGET_DIR="$HOME/.local/share/rewind"
    BIN_FILE="$HOME/.local/bin/rewind"
    DESKTOP_FILE="$HOME/.local/share/applications/rewind.desktop"
    ICON_FILE="$HOME/.local/share/icons/hicolor/256x256/apps/rewind.png"
    USER_DESK="$HOME/Desktop/rewind.desktop"
fi

# Stop any running instances
pkill -f rewind 2>/dev/null || true

rm -rf "$TARGET_DIR"
rm -f "$BIN_FILE"
rm -f "$DESKTOP_FILE"
rm -f "$USER_DESK"
rm -f "$ICON_FILE"
rm -f "$HOME/.config/autostart/rewind.desktop" 2>/dev/null || true

if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$(dirname "$DESKTOP_FILE")" 2>/dev/null || true
fi

echo -e "${GREEN}✓ Rewind został odinstalowany z systemu.${NC}"
echo -e "Katalog z nagranymi powtórkami ($HOME/Videos/Rewind/) został zachowany."
