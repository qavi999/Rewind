#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Uninstaller Launcher
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
    chmod +x "$SCRIPT_DIR/RewindSetup" 2>/dev/null || true
    exec "$SCRIPT_DIR/RewindSetup" --uninstall "$@"
elif [ -f "$SCRIPT_DIR/installer.py" ]; then
    exec python3 "$SCRIPT_DIR/installer.py" --uninstall "$@"
else
    echo "Błąd: Nie znaleziono pliku RewindSetup w $SCRIPT_DIR!" >&2
    exit 1
fi
