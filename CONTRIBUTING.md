# Contributing to Rewind

Thank you for your interest in contributing to Rewind!

## Core Architecture Principles
1. **Zero External Dependencies**: Rewind compiles into an independent single binary. Avoid adding external NuGet packages or DLL requirements unless strictly required.
2. **Stock Compiler Compatibility**: The project must remain buildable using the built-in Microsoft .NET Framework C# compiler (`csc.exe`) via `src\build.ps1`.
3. **Hardware Acceleration First**: Any video buffering changes should maintain zero CPU overhead using GPU hardware encoding.
4. **Non-Intrusive HUD & Hotkeys**: In-game notifications must never steal keyboard or mouse focus from full-screen games.

## Building & Testing
Run the PowerShell build script locally to verify your changes:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "src\build.ps1"
```
Ensure both `Rewind.exe` and `RewindSetup.exe` compile with zero errors.

## Reporting Bugs
When opening an issue on GitHub, please specify:
- Windows version (Windows 10 / Windows 11)
- Graphics card model (NVIDIA GeForce series)
- Audio configuration (sample rate, microphone hardware)
- Steps to reproduce the issue
