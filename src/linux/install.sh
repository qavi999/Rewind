#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Installer Launcher
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
    chmod +x "$SCRIPT_DIR/RewindSetup" 2>/dev/null || true
    exec "$SCRIPT_DIR/RewindSetup" "$@"
elif [ -f "$SCRIPT_DIR/installer.py" ]; then
    exec python3 "$SCRIPT_DIR/installer.py" "$@"
else
    echo "Błąd: Nie znaleziono pliku RewindSetup w $SCRIPT_DIR!" >&2
    exit 1
fi
