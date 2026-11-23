# Rewind v0.2 — Cross-Platform Linux, Secondary Replay & Creator Suite

**Rewind v0.2** is a major feature update delivering complete Linux operating system support, a secondary replay shortcut for long clips, lossless video trimming, multi-track audio isolation, advanced numeric tuning (including custom volume boost up to 1000%), and automatic disk storage management.

---

## ⚡ What's New in v0.2

### 🐧 Cross-Platform Linux Support
- **PipeWire & PulseAudio Integration:** Seamless capture of desktop audio and microphone on modern Linux distributions.
- **X11 & Wayland Ready:** Global hotkeys via X11 hooks, with dedicated CLI commands (`rewind --save`, `rewind --save-long`) for Wayland (GNOME, KDE, Sway).
- **All-In-One Linux Package:** Distributed with `install.sh`, `uninstall.sh`, `.desktop` launcher, and `Rewind-v0.2-Linux.tar.gz`.

### 🎯 Secondary Hotkey (Long Clips)
- Added an independent secondary hotkey (e.g. `F7`, `F11`, `Alt+F8`) with dedicated duration (60s, 120s, 180s, 300s).
- Capture an entire competitive round with one shortcut, and quick highlights with your primary hotkey.

### ⚙️ Advanced Settings Mode
- Switch from fixed pills and sliders to freeform numeric inputs:
  - **Volume Gain (0% – 1000%):** Amplify quiet microphones or soft game sounds far beyond standard limits without distortion.
  - **Custom FPS:** Set any target framerate from 15 FPS up to 360 FPS.
  - **Custom Duration:** Set any buffer length from 5 to 600 seconds.
  - **Custom Bitrate:** Fine-tune video bandwidth from 2 Mbps up to 150 Mbps.
  - **Custom Hotkeys:** Full arbitrary hotkey combinations.

### ✂️ Lossless In-App Clip Trimmer
- Instantly trim start and end times right inside Rewind.
- Exports a cut MP4 in sub-seconds using stream copy without re-encoding quality loss.

### 💾 Smart Storage Management
- Set maximum folder size limits (10 GB, 25 GB, 50 GB, 100 GB) to prevent storage exhaustion.
- Automatic retention policy: automatically clean clips older than 7, 14, 30, or 60 days.

### 🎧 Multi-track Audio Channels
- Record system audio (Track 1) and microphone (Track 2) into separate streams inside the MP4 container for independent volume leveling during post-production.

### 🎨 UX & Accessibility Polish
- **Double-Click Preview:** Double-clicking any card in the clips library immediately plays the clip in your default media player.
- **Delete Key Shortcut:** Press `Delete` on selected clip cards to remove them quickly.
- **Full 6-Language Localization:** Polish, English, German, Spanish, French, and Ukrainian.
- **Light Theme Refinements:** Enhanced contrast and typography across the Raycast UI.
- **Windows Explorer Details:** Added official `0.2.0.0` version and product metadata to executables.

---

## 📦 Assets & Downloads

| Asset | Platform | Description |
|---|---|---|
| **`RewindSetup.exe`** | Windows 10/11 | Dedicated Windows installer with Start Menu, desktop shortcuts, and autostart support. |
| **`Rewind.exe`** | Windows 10/11 | Standalone portable executable. Zero installation required. |
| **`Rewind-v0.2-Linux.tar.gz`** | Linux (x86_64) | Complete Linux release tarball with automated installer, launcher, and daemon. |
| **`rewind-linux`** | Linux (x86_64) | Standalone Linux executable binary. |
| **`install.sh` / `uninstall.sh`** | Linux | Scripts for system-wide or user-level installation and removal. |
| **`rewind.desktop`** | Linux | XDG Desktop Entry specification file. |
