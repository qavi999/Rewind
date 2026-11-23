#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Uninstaller
# =============================================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}=== Deinstalator Rewind dla Linux ===${NC}"

if [ "$EUID" -eq 0 ]; then
    BIN_FILE="/usr/local/bin/rewind"
    DESKTOP_FILE="/usr/share/applications/rewind.desktop"
    ICON_FILE="/usr/share/icons/hicolor/256x256/apps/rewind.png"
else
    BIN_FILE="$HOME/.local/bin/rewind"
    DESKTOP_FILE="$HOME/.local/share/applications/rewind.desktop"
    ICON_FILE="$HOME/.local/share/icons/hicolor/256x256/apps/rewind.png"
fi

# Stop any running instances
killall rewind 2>/dev/null || true

rm -f "$BIN_FILE"
rm -f "$DESKTOP_FILE"
rm -f "$ICON_FILE"
rm -f "$HOME/.config/autostart/rewind.desktop" 2>/dev/null || true

if command -v update-desktop-database &> /dev/null; then
    update-desktop-database "$(dirname "$DESKTOP_FILE")" 2>/dev/null || true
fi

echo -e "${GREEN}✓ Rewind został odinstalowany z systemu.${NC}"
echo -e "Katalog z nagranymi powtórkami ($HOME/Videos/Rewind/) został zachowany."
