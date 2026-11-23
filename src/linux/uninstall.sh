#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Uninstaller Launcher
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# W-01: Prevent root pollution if user executed via `sudo ./uninstall.sh`
if [ "$EUID" -eq 0 ] && [ -n "$SUDO_USER" ]; then
    echo "[Rewind] Wykryto uruchomienie z uprawnieniami roota przez sudo."
    echo "[Rewind] Uruchamianie deinstalatora w kontekście użytkownika $SUDO_USER..."
    exec sudo -u "$SUDO_USER" "$0" "$@"
fi

# W-02: Cleanly stop running processes and cleanup runtime sockets
pkill -f "([r]ewind|[r]ewind-linux|[R]ewindSetup)" 2>/dev/null || true
rm -f "$XDG_RUNTIME_DIR/rewind*.sock" "$XDG_RUNTIME_DIR/rewind*.lock" /tmp/rewind*.sock /tmp/rewind*.lock 2>/dev/null || true

if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
    chmod +x "$SCRIPT_DIR/RewindSetup" 2>/dev/null || true
    exec "$SCRIPT_DIR/RewindSetup" --uninstall "$@"
elif [ -f "$SCRIPT_DIR/installer.py" ]; then
    exec python3 "$SCRIPT_DIR/installer.py" --uninstall "$@"
else
    echo "Błąd: Nie znaleziono pliku RewindSetup w $SCRIPT_DIR!" >&2
    exit 1
fi
