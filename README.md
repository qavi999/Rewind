<div align="center">

<img src="assets/logo.png" width="112" height="112" alt="Rewind Logo" style="border-radius: 24px; margin-bottom: 12px;" />

# Rewind

**High-precision, zero-latency instant replay buffer engineered for competitive gaming and clean system setups.**

[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)](https://github.com/qavi999/Rewind)
[![Capture: DirectX DXGI](https://img.shields.io/badge/Capture-DirectX%20Desktop%20Duplication-8B5CF6)](https://github.com/qavi999/Rewind)
[![Hardware: NVENC / AMF / QSV](https://img.shields.io/badge/Hardware-NVENC%20%7C%20AMF%20%7C%20QSV-76B900)](https://github.com/qavi999/Rewind)
[![License: Open Source MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Installer: Multi-Language](https://img.shields.io/badge/Download-RewindSetup.exe-8B5CF6?logo=windows&logoColor=white)](https://github.com/qavi999/Rewind/releases)

</div>

---

## ⚡ The Philosophy

Traditional game recording and clipping tools are bloated: heavy background overlays, mandatory accounts, background telemetry, and CPU utilization that degrades frametimes and introduces input lag during competitive play.

**Rewind is 100% free, open-source, and engineered strictly for maximum performance.**

It runs silently in your Windows system tray, capturing the exact gameplay moments you want the millisecond you hit your hotkey without sacrificing a single frame.

---

## 📊 Ultra-Low Resource Architecture

- **⚡ Hardware-Accelerated DirectX Capture (DXGI):** Grabs frames directly from GPU VRAM via Desktop Duplication API. Completely eliminates mouse cursor flicker and preserves full native 240Hz / 280Hz monitor refresh rates without DWM throttling.
- **🚀 < 0.5% CPU Overhead:** Offloads 100% of video encoding directly to dedicated GPU silicon (**NVIDIA NVENC**, **AMD AMF**, or **Intel QuickSync**), leaving your CPU completely free for gaming.
- **💾 Fixed ~80 MB RAM Ring Buffer:** Buffers gameplay entirely in high-speed system memory with a strictly bounded memory ceiling. Memory usage stays flat and never leaks over long gaming sessions.
- **🛡️ Zero SSD / Drive Thrashing:** Unlike tools that continuously write temporary video segments to your storage drive, Rewind only writes to disk when you explicitly press the clip hotkey, prolonging SSD lifespan and eliminating disk I/O micro-stutters.
- **⚡ Sub-100ms Instant Save:** Concatenates buffered video segments using instant stream-copy muxing (`-c copy`), delivering an MP4 file in milliseconds without re-encoding.
- **🚫 Zero Framework Bloat:** Pure native Windows architecture. No Electron, no Chromium wrappers, no Node.js runtime, and zero background analytics or telemetry.

---

## ✨ Features

- **🎮 Non-Activating Gaming HUD Toast:**
  - Discreet, semi-transparent notification appears when a clip is saved with duration and direct folder access.
  - Engineered with native Windows `WS_EX_NOACTIVATE` — **never steals keyboard/mouse focus or minimizes fullscreen games**.
- **🎧 Studio-Grade WASAPI Audio Loopback:**
  - High-fidelity 48kHz stereo loopback mixer capturing both game/system audio and microphone.
  - Real-time independent software volume sliders for system sound and microphone.
- **🔔 Procedural Sound Feedback:**
  - 4 lightweight procedural in-RAM sound presets (*Rewind, Shutter, Crystal Ding, 8-Bit Retro*).
  - Support for custom user `.wav` and `.mp3` audio files with instant live preview.
- **🖥️ Multi-Monitor Screen Selector:**
  - Choose which monitor to capture directly from settings with live display device detection.
- **🎨 Modern Dark & Light Interface:**
  - Distraction-free Raycast-inspired interface with seamless Dark and Light themes.
  - Native Windows 11 DWM title bar styling and system tray management.
- **🌐 All-In-One Multi-Language Setup:**
  - Distributed via a standalone installer (`RewindSetup.exe`) with on-the-fly language switching (*English, Polski, Deutsch, Español, Français, Українська*).
  - Automatically creates Start Menu shortcuts with native Windows Search indexing and optional silent autostart.
- **🔒 100% Private & Open Source:**
  - Zero analytics, no telemetry, no background updaters, and no registration required.

---

## ⌨️ Hotkeys & Configuration

| Setting | Default | Configurable Options |
|---|---|---|
| **Save Clip Hotkey** | `F8` | `F8`, `F9`, `F10`, `Alt+C` |
| **Buffer Duration** | `30s` | `15s`, `30s`, `60s`, `120s` |
| **Framerate** | `60 FPS` | `30 FPS`, `60 FPS` |
| **Video Bitrate** | `12 Mbps` | `8 Mbps`, `12 Mbps`, `16 Mbps` |
| **Display Selector** | Primary | Any connected display / monitor |
| **Capture Cursor** | Enabled | Enabled / Disabled |

---

## 🚀 Download & Installation

1. Download **`RewindSetup.exe`** from [Releases](https://github.com/qavi999/Rewind/releases).
2. Choose your language, select installation options, and click **Install**.
3. Rewind will register with Windows Search, create desktop/Start Menu shortcuts, and launch silently in your system tray.
4. Press `F8` (or your configured hotkey) in any game or desktop to capture your clip instantly.

---

## 📄 License

Rewind is free and open-source software licensed under the [MIT License](LICENSE).
