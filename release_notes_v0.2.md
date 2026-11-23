# Rewind v0.2 — Cross-Platform Linux, Secondary Replay & Creator Suite

**Rewind v0.2** is a major feature update delivering complete Linux operating system support, a secondary replay shortcut for long clips, lossless video trimming, multi-track audio isolation, advanced numeric tuning (including custom volume boost up to 1000%), and automatic disk storage management.

---

## ⚡ What's New in v0.2

### 🐧 Linux UI & Engine Transformation (Pure Jet Black Parity)
- **Visual Design Parity with Windows:** Redesigned Linux UI to match the Windows Raycast aesthetic down to the pixel:
  - Deep Jet Black canvas (`#0A0A0A`), sleek sidebar (`#0F0F0F`), and subtle cards (`#121214`) with 1px borders (`#1E1E24`) and Electric Coral Red (`#FF5757`).
  - Automatic modern antialiased typography detection (`Inter`, `Roboto`, `Ubuntu`, `Cantarell`, `DejaVu Sans`).
  - Breadcrumb header bar with minimize (`—`) and close (`✕`) buttons.
  - Discreet toast notifications for saved clips with quick play and folder links.
  - **Double-Click Preview:** Double-clicking any card immediately launches the clip in your default Linux media player.
  - **Delete Key Shortcut:** Press `Delete` on selected cards to quickly remove clips.
- **100% Non-Blocking Responsiveness:**
  - Decoupled all UI actions (`FPS`, `Bitrate`, `Duration`, `Volume`, `Buffer Toggle`) from blocking process operations.
  - In-place instant pill transitions with zero tab flickering or delays.
  - Background asynchronous clip trimmer with live progress state.
  - Watchdog logging to `~/.config/rewind/ffmpeg.log` with retry backoff to eliminate any CPU freezing or UI stutter.
- **Graphical Windowed Setup (`RewindSetup`):**
  - Standalone installer executable with dark theme, real-time 6-language switcher, directory selector, and desktop shortcuts.
  - Added `Rewind-Installer.desktop` allowing direct double-click installation from graphical file managers (GNOME Files, Dolphin, Thunar).
  - Robust display auto-detection (`DISPLAY` and `WAYLAND_DISPLAY`) even when executed through `sudo`.

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

---

## 📦 Assets & Downloads

| Asset | Platform | Description |
|---|---|---|
| **`RewindSetup.exe`** | Windows 10/11 | Official Windows installer (Start Menu, desktop shortcuts, Windows Search indexing, tray autostart). |
| **`Rewind-v0.2-Linux.tar.gz`** | Linux (x86_64) | Complete Linux release package with windowed `RewindSetup` installer, `Rewind-Installer.desktop`, and launch scripts. |
