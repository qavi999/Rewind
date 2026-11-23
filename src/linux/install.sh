#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Installer Launcher
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# W-01: Prevent root pollution if user executed via `sudo ./install.sh`
if [ "$EUID" -eq 0 ] && [ -n "$SUDO_USER" ]; then
    echo "[Rewind] Wykryto uruchomienie z uprawnieniami roota przez sudo."
    echo "[Rewind] Uruchamianie instalatora w kontekście użytkownika $SUDO_USER..."
    exec sudo -u "$SUDO_USER" "$0" "$@"
fi

# W-09: Verify tkinter availability on Debian/Ubuntu/Fedora/Arch
if ! python3 -c "import tkinter" &>/dev/null; then
    echo -e "\033[0;33m[Rewind] Uwaga: W systemie nie wykryto modułu python3-tk (wymaganego do okna GUI instalatora).\033[0m"
    if command -v apt-get &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo apt install -y python3-tk"
    elif command -v dnf &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo dnf install -y python3-tkinter"
    elif command -v pacman &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo pacman -S --noconfirm tk"
    fi
    echo "[Rewind] Próba uruchomienia w trybie graficznym lub automatyczny fallback CLI..."
fi

if [ -f "$SCRIPT_DIR/RewindSetup" ]; then
    chmod +x "$SCRIPT_DIR/RewindSetup" 2>/dev/null || true
    exec "$SCRIPT_DIR/RewindSetup" "$@"
elif [ -f "$SCRIPT_DIR/installer.py" ]; then
    exec python3 "$SCRIPT_DIR/installer.py" "$@"
else
    echo "Błąd: Nie znaleziono pliku RewindSetup w $SCRIPT_DIR!" >&2
    exit 1
fi
