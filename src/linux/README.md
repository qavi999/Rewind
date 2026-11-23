# Rewind for Linux (v0.2)

Błyskawiczne nagrywanie powtórek w buforze kołowym (Instant Replay) dla systemów Linux (X11 & Wayland).

## Wymagania systemowe
- **Python 3** z biblioteką Tkinter (dostępne domyślnie w systemie):
  - Ubuntu/Debian/Mint: `sudo apt install python3 python3-tk ffmpeg`
  - Arch/Manjaro: `sudo pacman -S python tk ffmpeg`
  - Fedora: `sudo dnf install python3 python3-tkinter ffmpeg`

## Instalacja

### 1. Nowoczesny instalator okienkowy (GUI Setup)
1. Rozpakuj pobrane archiwum:
   ```bash
   tar -xzf Rewind-v0.2-Linux.tar.gz
   cd Rewind-v0.2-Linux
   ```
2. Kliknij dwukrotnie plik **`Zainstaluj Rewind`** (`Rewind-Installer.desktop`) lub **`RewindSetup`** w menedżerze plików, albo wpisz w terminalu (bez sudo):
   ```bash
   ./install.sh
   ```
   *(Możesz również uruchomić `./RewindSetup` bezpośrednio – okno instalatora otworzy się natychmiast).*

3. W oknie instalatora wybierz język, folder docelowy oraz opcje skrótów i kliknij **„Zainstaluj Rewind”**. Program natychmiast utworzy skróty i uruchomi aplikację.

### 2. Instalacja w trybie terminalowym (Headless / Serwery)
Jeśli instalujesz w czystym terminalu bez środowiska graficznego:
```bash
./install.sh --cli
```

## Sposób użycia
- **Uruchomienie okna programu:** kliknij ikonę **Rewind** na Pulpicie, w Menu Aplikacji (Dash) lub wpisz `rewind` w terminalu.
- **Natychmiastowy zapis powtórki:** `rewind --save` (lub skrót globalny **F8** na X11).
- **Zapis dłuższej powtórki (drugi skrót):** `rewind --save-long` (lub skrót globalny **F7** na X11).
- **Przełączenie stanu bufora (Pauza/Wznów):** `rewind --toggle`
- **Status bufora:** `rewind --status`

## Nowe funkcje v0.2:
- 🖥️ **Nowoczesny instalator okienkowy:** Elegancki kreator instalacji z wyborem języków, katalogu, tworzeniem skrótu na Pulpicie i automatycznym uruchomieniem.
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
