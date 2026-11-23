# Rewind v0.2 — Cross-Platform Linux, Secondary Replay & Creator Suite

**Rewind v0.2** is a major feature update delivering complete Linux operating system support, a secondary replay shortcut for long clips, lossless video trimming, multi-track audio isolation, advanced numeric tuning (including custom volume boost up to 1000%), and automatic disk storage management.

---

## ⚡ What's New in v0.2

### 🐧 Cross-Platform Linux Hardening & Architecture
- **Watchdog Deadlock Prevention:** Upgraded buffer locking to re-entrant `threading.RLock()`, guaranteeing flawless crash recovery without deadlocks.
- **Clean Ring Buffer Truncation:** Excluded active recording segments during export to ensure MP4 clips have 100% valid PTS/DTS timestamps and zero corrupt endings.
- **Multi-Track Audio Concat:** Added full `-map 0` stream mapping so both system audio (Track 1) and microphone (Track 2) are preserved in output clips.
- **Resilient Audio Source Validation:** Gracefully falls back to system audio if microphone is disconnected or unavailable instead of failing the recording.
- **Hardware Acceleration Parity:** Clean encoder pipelines for NVENC (`h264_nvenc`), Intel QSV (`h264_qsv`), VAAPI (`h264_vaapi`), and CPU (`libx264`).
- **Secure Runtime IPC:** Shifted IPC sockets to isolated `$XDG_RUNTIME_DIR` (`0o600` permissions) with automatic headless daemon takeover when opening the GUI.
- **X11 Hotkey Safety:** Installed custom `XSetErrorHandler` to safely handle `BadAccess` key conflicts without process termination.
- **Asynchronous UI Trimmer:** Clip trimming runs in background worker threads, keeping Tkinter UI 100% responsive.
- **Root Pollution Prevention:** `install.sh` and `uninstall.sh` automatically detect `sudo` execution and run in user context without polluting `/root/`.

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

### 🎨 UX & Accessibility Polish
- **Double-Click Preview:** Double-clicking any card in the clips library immediately plays the clip in your default media player.
- **Full 6-Language Localization:** Polish, English, German, Spanish, French, and Ukrainian.
- **Embedded Setup Architecture:** Linux release distributes a clean standalone `RewindSetup` installer containing the embedded application payload.

---

## 📦 Assets & Downloads

| Asset | Platform | Description |
|---|---|---|
| **`RewindSetup.exe`** | Windows 10/11 | Official Windows installer (Start Menu, desktop shortcuts, Windows Search indexing, tray autostart). |
| **`Rewind-v0.2-Linux.tar.gz`** | Linux (x86_64) | Complete Linux release package with windowed `RewindSetup` installer and launch scripts. |
