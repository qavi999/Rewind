# Rewind for Linux (v0.2)

Natychmiastowe nagrywanie powtórek w buforze kołowym (Instant Replay) dla systemów Linux (X11 & Wayland).

## Wymagania systemowe
- **Python 3** (dostępny domyślnie na każdej dystrybucji)
- **FFmpeg** z obsługą PulseAudio / PipeWire:
  - Ubuntu/Debian/Mint: `sudo apt install ffmpeg`
  - Arch/Manjaro: `sudo pacman -S ffmpeg`
  - Fedora: `sudo dnf install ffmpeg`

## Szybka instalacja
1. Rozpakuj archiwum:
   ```bash
   tar -xzf Rewind-v0.2-Linux.tar.gz
   cd Rewind-v0.2-Linux
   ```
2. Uruchom instalator:
   ```bash
   chmod +x install.sh
   ./install.sh
   ```

## Sposób użycia
- Uruchomienie okna programu: `rewind` lub kliknięcie ikony **Rewind** w menu aplikacji.
- Natychmiastowy zapis powtórki: `rewind --save` (lub skrót globalny **F8** na X11).
- Zapis dłuższej powtórki (drugi skrót): `rewind --save-long` (lub skrót globalny **F7** na X11).
- Przełączenie stanu bufora (Pauza/Wznów): `rewind --toggle`
- Status bufora: `rewind --status`

## Nowe funkcje v0.2:
- ✂️ **Szybki mini-edytor (Quick Trimmer):** Przycinaj zapisane powtórki w ułamku sekundy bez utraty jakości (stream copy).
- 🎙️ **Multi-track Audio:** Zapisuj dźwięk gry i mikrofon na dwóch osobnych ścieżkach w pliku MP4.
- ⚡ **Dwa niezależne skróty powtórek:** Krótki klip (np. 30s) i długi klip (np. 120s / runda).
- 💾 **Inteligentny zarządca dysku:** Automatyczne czyszczenie najstarszych klipów po przekroczeniu limitu GB lub określonego wieku (dni).

### Konfiguracja skrótu na Waylandzie (GNOME, KDE Plasma, Hyprland, Sway)
Środowiska Wayland ze względów bezpieczeństwa ograniczają nasłuchiwanie klawiszy w tle. Aby wyzwalać powtórki dowolnym klawiszem:
1. Otwórz **Ustawienia systemowe** -> **Klawiatura** -> **Skróty klawiszowe**.
2. Dodaj skrót niestandardowy:
   - **Nazwa:** Rewind Replay
   - **Polecenie:** `rewind --save`
   - **Klawisz:** `F8` (lub dowolny ulubiony)
