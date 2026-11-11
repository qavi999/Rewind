<div align="center">

# ✦ Rewind

**High-precision, zero-latency instant replay buffer engineered for competitive gaming and clean system setups.**

[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)](https://github.com/qavi999/Rewind)
[![Encoder: NVENC](https://img.shields.io/badge/Hardware-NVIDIA%20NVENC-76B900?logo=nvidia&logoColor=white)](https://github.com/qavi999/Rewind)
[![License: Open Source MIT](https://img.shields.io/badge/License-Open%20Source%20(MIT)-green.svg)](LICENSE)
[![Installer: Multi--Language](https://img.shields.io/badge/Download-RewindSetup.exe-8B5CF6?logo=windows&logoColor=white)](https://github.com/qavi999/Rewind/releases)

</div>

---

## ⚡ The Philosophy

Most clipping and replay tools today are bloated: heavy background overlays, forced accounts, telemetry, and background CPU drag that degrades frame times and introduces input lag in competitive games.

**Rewind is 100% free, open-source, and engineered strictly for performance.** 

It runs silently in your system tray, capturing the exact gameplay you want the millisecond you hit the key without sacrificing a single frame.

---

## 📊 Ultra-Low Resource Footprint

Rewind was built specifically to eliminate the overhead of traditional recording software:

- **⚡ < 0.5% CPU Overhead:** Offloads 100% of video encoding directly to dedicated NVIDIA NVENC hardware silicon, leaving your CPU untouched for pure game processing.
- **💾 Fixed ~80 MB RAM Usage:** Operates on an in-memory circular ring buffer with a strictly bounded memory ceiling. Memory usage stays flat and never leaks over long gaming sessions.
- **🛡️ Zero SSD / Drive Thrashing:** Unlike tools that constantly write video segments to your storage drive in the background, Rewind buffers frames purely in high-speed RAM. Your SSD is only written to when you explicitly press the clip hotkey, prolonging drive lifespan and preventing I/O micro-stutters.
- **🚫 Zero Framework Bloat:** No Electron, no Chromium wrappers, no Node.js runtimes, and zero background analytics or telemetry services. Just pure, native Windows performance.

---

## ✨ Features

- **⚡ Hardware-Accelerated NVENC Pipeline:** Video encoding runs entirely on dedicated GPU silicon, guaranteeing smooth frametimes in titles like CS2, Valorant, Fortnite, and Apex Legends.
- **🎛️ Complete Audio Customization:**
  - 4 procedural in-RAM synthesized sound presets (*Rewind, Shutter, Crystal Ding, 8-Bit Retro*) with zero disk latency.
  - Full support for custom `.wav` and `.mp3` audio files with instant live preview (`▶ Test Sound`).
  - Studio-grade WASAPI 48kHz loopback mixer with independent system and microphone volume controls.
- **🎯 Non-Activating HUD Overlay:**
  - Discreet notification appears when a clip is captured with duration and quick-open actions.
  - Built with native `WS_EX_NOACTIVATE` — **never steals keyboard/mouse focus or minimizes full-screen games**.
- **🎨 Modern Dark & Light Themes:**
  - Minimalist, distraction-free interface with instant hot-toggle between deep pitch-black and clean light modes.
  - Native Windows 11 DWM title bar integration.
- **🌐 All-In-One Multi-Language Setup:**
  - Distributed via a clean installer (`RewindSetup.exe`) with on-the-fly language switching (*English, Polski, Deutsch, Español, Français, Українська*).
  - Automatically creates Start Menu shortcuts with native Windows Search indexing and optional silent tray autostart.
- **🔒 100% Open Source & Private:**
  - Zero telemetry, no analytics, no background updaters, and no accounts required.

---

## ⌨️ Buffer & Hotkey Configuration

| Setting | Default | Configurable Options |
|---|---|---|
| **Save Clip Hotkey** | `F8` | `F8`, `F9`, `F10`, `Alt+C` |
| **Buffer Duration** | `30s` | `15s`, `30s`, `60s`, `120s` |
| **Framerate** | `60 FPS` | `30 FPS`, `60 FPS` |
| **Video Bitrate** | `12 Mbps` | `8 Mbps`, `12 Mbps`, `16 Mbps` |

---

## 🚀 Download & Installation

Rewind is distributed exclusively via its dedicated setup installer:

1. Download **`RewindSetup.exe`** from [Releases](https://github.com/qavi999/Rewind/releases).
2. Choose your language, select your installation options, and click **Install**.
3. Rewind will register with Windows Search, place shortcuts on your desktop/Start menu, and launch silently in the system tray.
4. Press `F8` (or your configured hotkey) in any game to capture your clip instantly.

---

## 📄 License

Rewind is free and open-source software licensed under the [MIT License](LICENSE).
