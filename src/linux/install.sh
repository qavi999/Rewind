#!/usr/bin/env bash
# =============================================================================
# Rewind Linux Installer Launcher
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Auto-detect display if not exported (e.g. running from basic terminal or file manager)
if [ -z "$DISPLAY" ] && [ -z "$WAYLAND_DISPLAY" ]; then
    for x_num in 0 1 2; do
        if [ -e "/tmp/.X11-unix/X$x_num" ]; then
            export DISPLAY=":$x_num"
            break
        fi
    done
fi

# If executed via sudo, preserve user display environment and run in user's graphical context
if [ "$EUID" -eq 0 ] && [ -n "$SUDO_USER" ]; then
    USER_HOME=$(getent passwd "$SUDO_USER" | cut -d: -f6)
    export XAUTHORITY="${XAUTHORITY:-$USER_HOME/.Xauthority}"
    export DISPLAY="${DISPLAY:-:0}"
    exec sudo -E -u "$SUDO_USER" env "DISPLAY=$DISPLAY" "XAUTHORITY=$XAUTHORITY" "WAYLAND_DISPLAY=$WAYLAND_DISPLAY" "HOME=$USER_HOME" "$0" "$@"
fi

# Verify tkinter availability on Debian/Ubuntu/Fedora/Arch
if ! python3 -c "import tkinter" &>/dev/null; then
    echo -e "\033[0;33m[Rewind] Uwaga: W systemie nie wykryto modułu python3-tk (wymaganego do okna GUI instalatora).\033[0m"
    if command -v apt-get &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo apt install -y python3-tk"
    elif command -v dnf &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo dnf install -y python3-tkinter"
    elif command -v pacman &>/dev/null; then
        echo "[Rewind] Możesz go zainstalować poleceniem: sudo pacman -S --noconfirm tk"
    fi
    echo "[Rewind] Próba otwarcia okna instalatora graficznego..."
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
