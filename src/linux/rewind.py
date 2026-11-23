#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Rewind v0.2 for Linux
Instant replay and rolling buffer clip recorder.
Designed for Linux (X11 & Wayland) with hardware NVENC / VAAPI / QSV acceleration.
"""

import os
import sys
import re
import time
import json
import glob
import math
import shutil
import signal
import socket
import struct
import base64
import ctypes
import tempfile
import threading
import subprocess
import argparse
from pathlib import Path

# Try importing embedded assets
try:
    from assets_b64 import CLIP_WAV_B64, LOGO_PNG_B64
except ImportError:
    CLIP_WAV_B64 = ""
    LOGO_PNG_B64 = ""

APP_NAME = "Rewind"
APP_VERSION = "v0.2"
CONFIG_DIR = Path.home() / ".config" / "rewind"
CONFIG_FILE = CONFIG_DIR / "rewind_settings.json"
BUFFER_DIR = Path(tempfile.gettempdir()) / f"rewind_buffer_{os.getuid()}"
DEFAULT_CLIPS_DIR = Path.home() / "Videos" / "Rewind"

def get_runtime_dir():
    """Get secure user-isolated runtime directory for sockets and locks (W-05)."""
    xdg = os.environ.get("XDG_RUNTIME_DIR")
    if xdg and os.path.isdir(xdg) and os.access(xdg, os.W_OK):
        return Path(xdg)
    fallback = Path(tempfile.gettempdir()) / f"rewind_run_{os.getuid()}"
    try:
        fallback.mkdir(mode=0o700, parents=True, exist_ok=True)
    except Exception:
        pass
    return fallback

RUNTIME_DIR = get_runtime_dir()
SOCKET_FILE = RUNTIME_DIR / "rewind.sock"
LOCK_FILE = RUNTIME_DIR / "rewind.lock"


# =============================================================================
# SETTINGS
# =============================================================================
class AppSettings:
    def __init__(self):
        self.ClipDuration = 30
        self.Fps = 60
        self.Bitrate = "12M"
        self.Hotkey = "F8"
        self.ClipsFolder = str(DEFAULT_CLIPS_DIR)
        self.BufferActive = True
        self.PlaySound = True
        self.StartMinimized = False
        self.AutoStart = False
        self.CaptureCursor = False
        self.Theme = "Dark"
        self.SoundPreset = "Default"
        self.Language = "pl"
        self.SystemVolumePercent = 100
        self.MicVolumePercent = 100
        self.SelectedMicId = "default"
        self.AdvancedMode = False
        self.SeparateAudioTracks = False
        self.SecondaryHotkeyEnabled = False
        self.SecondaryHotkey = "F7"
        self.SecondaryClipDuration = 120
        self.StorageLimitEnabled = False
        self.MaxStorageLimitGB = 25
        self.MaxClipAgeDays = 0

    @classmethod
    def load(cls):
        s = cls()
        try:
            if CONFIG_FILE.exists():
                with open(CONFIG_FILE, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    for k, v in data.items():
                        if hasattr(s, k):
                            setattr(s, k, v)
        except Exception:
            pass

        # Sanitize
        if s.SystemVolumePercent < 0 or s.SystemVolumePercent > 1000:
            s.SystemVolumePercent = 100
        if s.MicVolumePercent < 0 or s.MicVolumePercent > 1000:
            s.MicVolumePercent = 100
        if s.ClipDuration < 5 or s.ClipDuration > 600:
            s.ClipDuration = 30
        if s.Fps < 15 or s.Fps > 360:
            s.Fps = 60
        if not s.Bitrate:
            s.Bitrate = "12M"
        if not s.Hotkey:
            s.Hotkey = "F8"
        if s.SecondaryClipDuration < 5 or s.SecondaryClipDuration > 600:
            s.SecondaryClipDuration = 120
        if not s.SecondaryHotkey:
            s.SecondaryHotkey = "F7"
        if s.MaxStorageLimitGB not in [10, 25, 50, 100]:
            s.MaxStorageLimitGB = 25
        if s.MaxClipAgeDays not in [0, 7, 14, 30, 60]:
            s.MaxClipAgeDays = 0

        try:
            os.makedirs(s.ClipsFolder, exist_ok=True)
        except Exception:
            pass
        return s

    def save(self):
        try:
            CONFIG_DIR.mkdir(parents=True, exist_ok=True)
            with open(CONFIG_FILE, "w", encoding="utf-8") as f:
                json.dump(self.__dict__, f, indent=2, ensure_ascii=False)
        except Exception as e:
            print(f"[Rewind] Błąd zapisu ustawień: {e}", file=sys.stderr)


# =============================================================================
# SYSTEM INTEGRATION HELPERS
# =============================================================================
def ensure_assets():
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    sound_path = CONFIG_DIR / "clip.wav"
    icon_path = CONFIG_DIR / "icon.png"

    if not sound_path.exists() and CLIP_WAV_B64:
        try:
            with open(sound_path, "wb") as f:
                f.write(base64.b64decode(CLIP_WAV_B64))
        except Exception:
            pass

    if not icon_path.exists() and LOGO_PNG_B64:
        try:
            with open(icon_path, "wb") as f:
                f.write(base64.b64decode(LOGO_PNG_B64))
        except Exception:
            pass

    return sound_path, icon_path


def play_sound():
    sound_path = CONFIG_DIR / "clip.wav"
    if not sound_path.exists():
        return

    def _worker():
        players = [
            ["paplay", str(sound_path)],
            ["pw-play", str(sound_path)],
            ["aplay", "-q", str(sound_path)],
            ["ffplay", "-nodisp", "-autoexit", "-loglevel", "quiet", str(sound_path)]
        ]
        for cmd in players:
            if shutil.which(cmd[0]):
                try:
                    subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=2)
                    return
                except Exception:
                    pass

    threading.Thread(target=_worker, daemon=True).start()


def show_notification(title, message):
    def _worker():
        icon_path = CONFIG_DIR / "icon.png"
        cmd = ["notify-send", "-a", "Rewind", "-t", "3000"]
        if icon_path.exists():
            cmd.extend(["-i", str(icon_path)])
        cmd.extend([title, message])
        if shutil.which("notify-send"):
            try:
                subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=2)
            except Exception:
                pass

    threading.Thread(target=_worker, daemon=True).start()


def detect_encoder():
    """Detect hardware H.264 encoder on Linux."""
    try:
        proc = subprocess.run(
            ["ffmpeg", "-hide_banner", "-encoders"],
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            timeout=3
        )
        out = proc.stdout
        if "h264_nvenc" in out:
            # Verify nvidia driver presence
            if shutil.which("nvidia-smi") or os.path.exists("/dev/nvidia0"):
                return "h264_nvenc"
        if "h264_vaapi" in out and os.path.exists("/dev/dri/renderD128"):
            return "h264_vaapi"
        if "h264_qsv" in out:
            return "h264_qsv"
    except Exception:
        pass
    return "libx264"


def detect_screen_resolution():
    """Detect current screen resolution and offset."""
    # Try xrandr
    if shutil.which("xrandr"):
        try:
            out = subprocess.check_output(["xrandr", "--current"], text=True, timeout=2)
            for line in out.splitlines():
                if " connected primary" in line or (" connected" in line and "+" in line):
                    m = re.search(r"(\d+)x(\d+)\+(\d+)\+(\d+)", line)
                    if m:
                        return int(m.group(1)), int(m.group(2)), int(m.group(3)), int(m.group(4))
        except Exception:
            pass

    # Try xdpyinfo
    if shutil.which("xdpyinfo"):
        try:
            out = subprocess.check_output(["xdpyinfo"], text=True, timeout=2)
            for line in out.splitlines():
                if "dimensions:" in line:
                    m = re.search(r"(\d+)x(\d+)\s+pixels", line)
                    if m:
                        return int(m.group(1)), int(m.group(2)), 0, 0
        except Exception:
            pass

    return 1920, 1080, 0, 0


def is_wayland_session():
    """Detect if current session is running under Wayland (GNOME, KDE, Hyprland, Sway)."""
    return bool(os.environ.get("WAYLAND_DISPLAY") or os.environ.get("XDG_SESSION_TYPE") == "wayland")


def check_audio_source_valid(src_name):
    """Check if pulse/pipewire audio source exists and can be captured (W-04)."""
    if not src_name or src_name == "none":
        return False
    if src_name == "default" or src_name.endswith(".monitor"):
        return True
    if shutil.which("pactl"):
        try:
            out = subprocess.check_output(["pactl", "list", "sources", "short"], text=True, timeout=2)
            for line in out.splitlines():
                parts = line.split()
                if len(parts) >= 2 and (parts[1] == src_name or src_name in parts[1]):
                    return True
        except Exception:
            pass
    return True


def detect_audio_devices():
    """Detect PulseAudio/PipeWire default output monitor and mic."""
    sys_monitor = "default.monitor"
    mic_source = "default"

    if shutil.which("pactl"):
        try:
            out = subprocess.check_output(["pactl", "get-default-sink"], text=True, timeout=2).strip()
            if out:
                sys_monitor = f"{out}.monitor"
        except Exception:
            pass
        try:
            out = subprocess.check_output(["pactl", "get-default-source"], text=True, timeout=2).strip()
            if out:
                mic_source = out
        except Exception:
            pass

    return sys_monitor, mic_source


# =============================================================================
# BUFFER ENGINE (FFMPEG ROLLING RING BUFFER)
# =============================================================================
class BufferEngine:
    def __init__(self, settings):
        self.settings = settings
        self.is_recording = False
        self.is_saving = False
        self.process = None
        # Use re-entrant RLock to prevent watchdog deadlock (Showstopper #2)
        self.lock = threading.RLock()
        self.encoder = detect_encoder()
        self.record_start_time = 0
        self.status_listeners = []
        self.clip_saving_listeners = []
        self.clip_saved_listeners = []
        self.clip_saved_done_listeners = []

        BUFFER_DIR.mkdir(parents=True, exist_ok=True)

        # Start watchdog thread
        self.watchdog_thread = threading.Thread(target=self._watchdog_loop, daemon=True)
        self.watchdog_thread.start()

    def add_status_listener(self, fn):
        self.status_listeners.append(fn)

    def add_clip_saving_listener(self, fn):
        self.clip_saving_listeners.append(fn)

    def add_clip_saved_listener(self, fn):
        self.clip_saved_listeners.append(fn)

    def add_clip_saved_done_listener(self, fn):
        self.clip_saved_done_listeners.append(fn)

    def notify_status(self, recording, text):
        self.is_recording = recording
        for fn in self.status_listeners:
            try:
                fn(recording, text)
            except Exception:
                pass

    def clean_buffer_dir(self):
        try:
            if BUFFER_DIR.exists():
                for f in BUFFER_DIR.glob("seg_*.ts"):
                    try:
                        f.unlink()
                    except Exception:
                        pass
                for f in BUFFER_DIR.glob("concat_*.txt"):
                    try:
                        f.unlink()
                    except Exception:
                        pass
        except Exception:
            pass

    def start(self):
        with self.lock:
            if self.is_recording:
                return

            if not shutil.which("ffmpeg"):
                self.notify_status(False, "Brak silnika FFmpeg (zainstaluj ffmpeg)")
                return

            self.clean_buffer_dir()
            w, h, ox, oy = detect_screen_resolution()
            fps = max(15, min(360, self.settings.Fps))
            bitrate = self.settings.Bitrate or "12M"
            draw_mouse = 1 if self.settings.CaptureCursor else 0
            seg_pattern = str(BUFFER_DIR / "seg_%06d.ts")

            sys_mon, mic_src = detect_audio_devices()
            sys_vol = max(0.0, self.settings.SystemVolumePercent / 100.0)
            mic_vol = max(0.0, self.settings.MicVolumePercent / 100.0)

            # Build FFmpeg command
            cmd = ["ffmpeg", "-y", "-nostats", "-loglevel", "error"]

            # Video input (X11 & Wayland XWayland bridge)
            display = os.environ.get("DISPLAY", ":0.0")
            cmd.extend([
                "-f", "x11grab",
                "-draw_mouse", str(draw_mouse),
                "-video_size", f"{w}x{h}",
                "-framerate", str(fps),
                "-i", f"{display}+{ox},{oy}"
            ])

            # Audio inputs with device validation (W-04)
            has_pulse = shutil.which("pactl") or shutil.which("pulseaudio") or shutil.which("pipewire")
            has_sys_audio = bool(has_pulse and check_audio_source_valid(sys_mon))
            has_mic_audio = bool(has_pulse and check_audio_source_valid(mic_src))

            filters = []
            audio_maps = []
            audio_input_idx = 1

            if has_sys_audio:
                cmd.extend(["-f", "pulse", "-i", sys_mon])
                sys_idx = audio_input_idx
                audio_input_idx += 1
            else:
                sys_idx = None

            if has_mic_audio:
                cmd.extend(["-f", "pulse", "-i", mic_src])
                mic_idx = audio_input_idx
                audio_input_idx += 1
            else:
                mic_idx = None

            # Setup audio filters & channels
            if has_sys_audio and has_mic_audio:
                if self.settings.SeparateAudioTracks:
                    filters.append(f"[{sys_idx}:a]volume={sys_vol:.2f}[a_sys]")
                    filters.append(f"[{mic_idx}:a]volume={mic_vol:.2f}[a_mic]")
                    audio_maps.extend([
                        "-map", "[a_sys]",
                        "-map", "[a_mic]",
                        "-metadata:s:a:0", "title=Game Audio",
                        "-metadata:s:a:1", "title=Microphone"
                    ])
                else:
                    filters.append(f"[{sys_idx}:a]volume={sys_vol:.2f}[a0]")
                    filters.append(f"[{mic_idx}:a]volume={mic_vol:.2f}[a1]")
                    filters.append(f"[a0][a1]amix=inputs=2:duration=first:dropout_transition=0[aout]")
                    audio_maps.extend(["-map", "[aout]"])
            elif has_sys_audio:
                filters.append(f"[{sys_idx}:a]volume={sys_vol:.2f}[aout]")
                audio_maps.extend(["-map", "[aout]"])
            elif has_mic_audio:
                filters.append(f"[{mic_idx}:a]volume={mic_vol:.2f}[aout]")
                audio_maps.extend(["-map", "[aout]"])

            # Video encoder & VAAPI hardware filter integration (W-08)
            v_map = "0:v"
            if self.encoder == "h264_vaapi":
                filters.append("[0:v]format=nv12,hwupload[vout]")
                v_map = "[vout]"
                cmd.extend([
                    "-vaapi_device", "/dev/dri/renderD128",
                    "-c:v", "h264_vaapi",
                    "-b:v", bitrate
                ])
            elif self.encoder == "h264_nvenc":
                cmd.extend([
                    "-c:v", "h264_nvenc",
                    "-preset", "p1",
                    "-tune", "ull",
                    "-b:v", bitrate,
                    "-maxrate", bitrate,
                    "-bufsize", "8M"
                ])
            elif self.encoder == "h264_qsv":
                cmd.extend([
                    "-c:v", "h264_qsv",
                    "-preset", "veryfast",
                    "-b:v", bitrate
                ])
            else:
                cmd.extend([
                    "-c:v", "libx264",
                    "-preset", "ultrafast",
                    "-tune", "zerolatency",
                    "-b:v", bitrate,
                    "-pix_fmt", "yuv420p"
                ])

            if filters:
                cmd.extend(["-filter_complex", ";".join(filters)])

            cmd.extend(["-map", v_map])

            if audio_maps:
                cmd.extend(audio_maps)
                cmd.extend(["-c:a", "aac", "-b:a", "192k"])

            cmd.extend([
                "-g", str(fps * 2),
                "-keyint_min", str(fps * 2),
                "-f", "segment",
                "-segment_time", "2",
                "-segment_start_number", "1",
                "-reset_timestamps", "0",
                seg_pattern
            ])

            FFMPEG_LOG = CONFIG_DIR / "ffmpeg.log"
            try:
                log_file = open(FFMPEG_LOG, "w", encoding="utf-8")
            except Exception:
                log_file = subprocess.DEVNULL

            try:
                self.process = subprocess.Popen(
                    cmd,
                    stdin=subprocess.PIPE,
                    stdout=subprocess.DEVNULL,
                    stderr=log_file
                )
                self.record_start_time = time.time()
                self.is_recording = True
                enc_name = self.encoder.replace("h264_", "").upper()
                sess_info = "Wayland" if is_wayland_session() else "X11"
                self.notify_status(True, f"Bufor aktywny ({enc_name} + Audio, {sess_info})")
            except Exception as e:
                self.is_recording = False
                self.notify_status(False, f"Błąd uruchamiania bufora: {e}")

    def stop(self):
        with self.lock:
            self.is_recording = False
            p = self.process
            self.process = None

        if p:
            try:
                p.terminate()
                p.wait(timeout=1.0)
            except Exception:
                try:
                    p.kill()
                    p.wait(timeout=1.0)  # Prevent zombie <defunct> processes (W-03)
                except Exception:
                    pass

        with self.lock:
            self.clean_buffer_dir()
            self.notify_status(False, "Bufor zatrzymany")

    def _watchdog_loop(self):
        fail_count = 0
        while True:
            time.sleep(2.5)
            try:
                with self.lock:
                    if not self.settings.BufferActive or self.is_saving:
                        continue

                    # Revive if dead with backoff
                    if self.is_recording:
                        if self.process is None or self.process.poll() is not None:
                            fail_count += 1
                            if fail_count >= 4:
                                self.is_recording = False
                                self.notify_status(False, "Bufor zatrzymany (błąd startu FFmpeg - sprawdź ~/.config/rewind/ffmpeg.log)")
                                time.sleep(10)
                                fail_count = 0
                                continue
                            self.stop()
                            self.start()
                            continue
                        else:
                            fail_count = 0

                        # Check for freeze
                        if time.time() - self.record_start_time > 8.0:
                            files = list(BUFFER_DIR.glob("seg_*.ts"))
                            if files:
                                newest = max(files, key=lambda f: f.stat().st_mtime)
                                if time.time() - newest.stat().st_mtime > 10.0:
                                    self.stop()
                                    self.start()
                                    continue

                    # Trim ring buffer
                    self._trim_buffer()
            except Exception:
                pass

    def _trim_buffer(self):
        files = sorted(BUFFER_DIR.glob("seg_*.ts"), key=lambda f: f.name)
        max_dur = self.settings.ClipDuration
        if self.settings.SecondaryHotkeyEnabled and self.settings.SecondaryClipDuration > max_dur:
            max_dur = self.settings.SecondaryClipDuration

        needed_count = math.ceil(max_dur / 2.0) + 1
        keep_count = needed_count + 6

        if len(files) > keep_count:
            for f in files[:-keep_count]:
                try:
                    f.unlink()
                except Exception:
                    pass

    def save_clip(self, custom_duration=0):
        with self.lock:
            if self.is_saving:
                return
            if not self.is_recording:
                show_notification("Rewind", "Bufor nie jest aktywny. Trwa wznawianie...")
                self.start()
                return
            self.is_saving = True

        target_dur = custom_duration if custom_duration > 0 else self.settings.ClipDuration

        # Natychmiastowy dźwięk potwierdzenia przy wyzwoleniu powtórki (0 ms opóźnienia)
        if self.settings.PlaySound:
            play_sound()

        for fn in self.clip_saving_listeners:
            try:
                fn(target_dur)
            except Exception:
                pass

        def _worker():
            try:
                self._execute_save_clip(target_dur)
            except Exception as e:
                show_notification("Rewind", f"Błąd zapisu klipu: {e}")
            finally:
                with self.lock:
                    self.is_saving = False
                for fn in self.clip_saved_done_listeners:
                    try:
                        fn()
                    except Exception:
                        pass

        threading.Thread(target=_worker, daemon=True).start()

    def _execute_save_clip(self, custom_duration=0):
        raw_files = sorted(
            [f for f in BUFFER_DIR.glob("seg_*.ts") if f.stat().st_size > 2048],
            key=lambda f: f.name
        )

        if not raw_files:
            show_notification("Rewind", "Brak nagranych danych w buforze (odczekaj kilka sekund).")
            return

        # Exclude active segment being currently written by FFmpeg to prevent EOF / PTS glitches (Showstopper #3)
        if len(raw_files) >= 2:
            completed_files = raw_files[:-1]
        else:
            completed_files = raw_files

        newest = max(completed_files, key=lambda f: f.stat().st_mtime)
        if time.time() - newest.stat().st_mtime > 8.0:
            show_notification("Rewind", "Bufor nie zawierał świeżych klatek. Trwa restart bufora.")
            self.stop()
            self.start()
            return

        target_dur = custom_duration if custom_duration > 0 else self.settings.ClipDuration
        needed_count = math.ceil(target_dur / 2.0)
        chosen = completed_files[-needed_count:]

        if not chosen:
            show_notification("Rewind", "Bufor przygotowuje pierwsze segmenty, spróbuj za 2 sekundy.")
            return

        # Create concat list
        concat_file = BUFFER_DIR / f"concat_{int(time.time()*1000)}.txt"
        with open(concat_file, "w", encoding="utf-8") as f:
            for seg in chosen:
                f.write(f"file '{seg.resolve()}'\n")

        clips_dir = Path(self.settings.ClipsFolder)
        clips_dir.mkdir(parents=True, exist_ok=True)

        timestamp = time.strftime("%Y-%m-%d_%H-%M-%S")
        output_clip = clips_dir / f"Clip_{timestamp}.mp4"

        # Concat with -map 0 to preserve all audio tracks including microphone (Showstopper #4)
        cmd = [
            "ffmpeg", "-y", "-nostats", "-loglevel", "error",
            "-f", "concat", "-safe", "0",
            "-i", str(concat_file),
            "-map", "0",
            "-c", "copy",
            "-fflags", "+genpts",
            "-avoid_negative_ts", "make_zero",
            str(output_clip)
        ]

        subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=15)

        try:
            concat_file.unlink()
        except Exception:
            pass

        if output_clip.exists() and output_clip.stat().st_size > 0:
            dur = len(chosen) * 2
            show_notification("Rewind", f"⚡ Zapisano klip ({dur}s): {output_clip.name}")

            # Enforce storage limits
            self.enforce_storage_limits()

            for fn in self.clip_saved_listeners:
                try:
                    fn(str(output_clip), dur)
                except Exception:
                    pass
        else:
            show_notification("Rewind", "Nie udało się wygenerować pliku MP4.")

    def trim_clip(self, input_path, start_sec, end_sec):
        """Trim clip without re-encoding using FFmpeg stream copy."""
        inp = Path(input_path)
        if not inp.exists():
            return False, "Plik źródłowy nie istnieje."

        out_name = f"{inp.stem}_trim_{int(start_sec)}s-{int(end_sec)}s{inp.suffix}"
        out_path = inp.parent / out_name
        dur = end_sec - start_sec
        if dur <= 0:
            return False, "Czas trwania musi być większy niż 0."

        cmd = [
            "ffmpeg", "-y", "-nostats", "-loglevel", "error",
            "-ss", str(start_sec),
            "-i", str(inp),
            "-t", str(dur),
            "-c", "copy",
            "-avoid_negative_ts", "make_zero",
            "-movflags", "+faststart",
            str(out_path)
        ]
        try:
            res = subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=15)
            if res.returncode == 0 and out_path.exists() and out_path.stat().st_size > 0:
                return True, str(out_path)
        except Exception as e:
            return False, str(e)
        return False, "Błąd FFmpeg podczas przycinania."

    def enforce_storage_limits(self):
        """Enforces max storage quota and max file age on clips folder."""
        if not self.settings.StorageLimitEnabled:
            return

        clips_dir = Path(self.settings.ClipsFolder)
        if not clips_dir.exists():
            return

        try:
            files = [f for f in clips_dir.glob("Clip_*.mp4") if f.is_file()]
            if not files:
                return

            now = time.time()

            # 1. Age-based cleanup
            if self.settings.MaxClipAgeDays > 0:
                cutoff = now - (self.settings.MaxClipAgeDays * 86400)
                remaining = []
                for f in files:
                    try:
                        if f.stat().st_mtime < cutoff:
                            f.unlink()
                        else:
                            remaining.append(f)
                    except Exception:
                        remaining.append(f)
                files = remaining

            # 2. Size-based cleanup (GB limit)
            max_bytes = self.settings.MaxStorageLimitGB * 1024 * 1024 * 1024
            total_size = sum(f.stat().st_size for f in files if f.exists())

            if total_size > max_bytes:
                files_sorted = sorted(files, key=lambda f: f.stat().st_mtime)
                for f in files_sorted:
                    if total_size <= max_bytes:
                        break
                    try:
                        sz = f.stat().st_size
                        f.unlink()
                        total_size -= sz
                    except Exception:
                        pass
        except Exception:
            pass


# =============================================================================
# SINGLE INSTANCE IPC & GLOBAL HOTKEYS
# =============================================================================
class IpcServer:
    def __init__(self, engine, on_show_window=None):
        self.engine = engine
        self.on_show_window = on_show_window
        self.sock = None
        self.running = False

    def start(self):
        try:
            if SOCKET_FILE.exists() or SOCKET_FILE.is_socket():
                SOCKET_FILE.unlink()
        except Exception:
            pass

        self.sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.sock.bind(str(SOCKET_FILE))
        try:
            os.chmod(str(SOCKET_FILE), 0o600)  # Secure socket permissions (W-05)
        except Exception:
            pass
        self.sock.listen(5)
        self.running = True

        threading.Thread(target=self._server_loop, daemon=True).start()

    def _server_loop(self):
        while self.running:
            try:
                conn, _ = self.sock.accept()
                threading.Thread(target=self._handle_client, args=(conn,), daemon=True).start()
            except Exception:
                break

    def _handle_client(self, conn):
        try:
            data = conn.recv(1024).decode("utf-8").strip()
            if data == "SAVE":
                self.engine.save_clip(self.engine.settings.ClipDuration)
                conn.sendall(b"OK:SAVED\n")
            elif data == "SAVE_LONG":
                self.engine.save_clip(self.engine.settings.SecondaryClipDuration)
                conn.sendall(b"OK:SAVED_LONG\n")
            elif data.startswith("SAVE:"):
                try:
                    dur_val = int(data.split(":")[1])
                    self.engine.save_clip(dur_val)
                    conn.sendall(b"OK:SAVED_CUSTOM\n")
                except Exception:
                    self.engine.save_clip()
                    conn.sendall(b"OK:SAVED\n")
            elif data == "TOGGLE":
                if self.engine.is_recording:
                    self.engine.stop()
                else:
                    self.engine.start()
                conn.sendall(b"OK:TOGGLED\n")
            elif data == "SHOW":
                if self.on_show_window:
                    self.on_show_window()
                    conn.sendall(b"OK:SHOWN\n")
                else:
                    # Daemon process running headless (Showstopper #5)
                    conn.sendall(b"ERR:HEADLESS\n")
            elif data == "STOP_FOR_GUI":
                if not self.on_show_window:
                    conn.sendall(b"OK:STOPPING\n")
                    def _stop_daemon():
                        time.sleep(0.3)
                        self.engine.stop()
                        os._exit(0)
                    threading.Thread(target=_stop_daemon, daemon=True).start()
                else:
                    conn.sendall(b"ERR:HAS_GUI\n")
            elif data == "STATUS":
                status = "ACTIVE" if self.engine.is_recording else "PAUSED"
                conn.sendall(f"STATUS:{status}:{self.engine.encoder}\n".encode("utf-8"))
        except Exception:
            pass
        finally:
            try:
                conn.close()
            except Exception:
                pass


def send_ipc_command(cmd):
    if not SOCKET_FILE.exists():
        return False
    try:
        s = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        s.connect(str(SOCKET_FILE))
        s.sendall(cmd.encode("utf-8"))
        res = s.recv(1024).decode("utf-8").strip()
        s.close()
        return res
    except Exception:
        return False


def start_x11_hotkey_listener(engine):
    """Listen for global hotkeys on X11 root window via XGrabKey with 64-bit ctypes safety."""
    def _worker():
        try:
            if not os.environ.get("DISPLAY"):
                return

            x11 = ctypes.cdll.LoadLibrary("libX11.so.6")

            # Thread safety for multi-threaded X11 calls
            try:
                x11.XInitThreads.restype = ctypes.c_int
                x11.XInitThreads.argtypes = []
                x11.XInitThreads()
            except Exception:
                pass

            # Define 64-bit pointer and type signatures to prevent SIGSEGV truncation
            x11.XOpenDisplay.restype = ctypes.c_void_p
            x11.XOpenDisplay.argtypes = [ctypes.c_char_p]

            x11.XDefaultRootWindow.restype = ctypes.c_ulong
            x11.XDefaultRootWindow.argtypes = [ctypes.c_void_p]

            x11.XKeysymToKeycode.restype = ctypes.c_uint
            x11.XKeysymToKeycode.argtypes = [ctypes.c_void_p, ctypes.c_ulong]

            x11.XGrabKey.restype = ctypes.c_int
            x11.XGrabKey.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_uint, ctypes.c_ulong, ctypes.c_int, ctypes.c_int, ctypes.c_int]

            x11.XSelectInput.restype = ctypes.c_int
            x11.XSelectInput.argtypes = [ctypes.c_void_p, ctypes.c_ulong, ctypes.c_long]

            x11.XNextEvent.restype = ctypes.c_int
            x11.XNextEvent.argtypes = [ctypes.c_void_p, ctypes.c_void_p]

            x11.XCloseDisplay.restype = ctypes.c_int
            x11.XCloseDisplay.argtypes = [ctypes.c_void_p]

            # Custom error handler so BadAccess hotkey conflicts don't terminate process (W-06)
            XErrorHandler = ctypes.CFUNCTYPE(ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p)
            def _x_error_handler(display, error_event):
                return 0
            _c_error_handler = XErrorHandler(_x_error_handler)
            try:
                x11.XSetErrorHandler.restype = ctypes.c_void_p
                x11.XSetErrorHandler.argtypes = [XErrorHandler]
                x11.XSetErrorHandler(_c_error_handler)
            except Exception:
                pass

            display = x11.XOpenDisplay(None)
            if not display:
                return

            root = x11.XDefaultRootWindow(display)
            if not root:
                try:
                    x11.XCloseDisplay(display)
                except Exception:
                    pass
                return

            # Map hotkey to keysym
            key_map = {
                "F7": 0xFFC4, "F8": 0xFFC5, "F9": 0xFFC6,
                "F10": 0xFFC7, "F11": 0xFFC8, "F12": 0xFFC9
            }
            primary_keysym = key_map.get(engine.settings.Hotkey.upper(), 0xFFC5)
            primary_code = x11.XKeysymToKeycode(display, primary_keysym)

            secondary_code = 0
            if engine.settings.SecondaryHotkeyEnabled and engine.settings.SecondaryHotkey:
                sec_keysym = key_map.get(engine.settings.SecondaryHotkey.upper(), 0xFFC4)
                secondary_code = x11.XKeysymToKeycode(display, sec_keysym)

            # Grab key with and without numlock / capslock masks
            modifiers = [0, 2, 16, 18]  # Mod2 = NumLock, Lock = CapsLock
            if primary_code != 0:
                for mod in modifiers:
                    x11.XGrabKey(display, primary_code, mod, root, True, 1, 1)

            if secondary_code != 0 and secondary_code != primary_code:
                for mod in modifiers:
                    x11.XGrabKey(display, secondary_code, mod, root, True, 1, 1)

            x11.XSelectInput(display, root, 0x00000001)  # KeyPressMask

            class XKeyEvent(ctypes.Structure):
                _fields_ = [
                    ("type", ctypes.c_int),
                    ("serial", ctypes.c_ulong),
                    ("send_event", ctypes.c_int),
                    ("display", ctypes.c_void_p),
                    ("window", ctypes.c_ulong),
                    ("root", ctypes.c_ulong),
                    ("subwindow", ctypes.c_ulong),
                    ("time", ctypes.c_ulong),
                    ("x", ctypes.c_int),
                    ("y", ctypes.c_int),
                    ("x_root", ctypes.c_int),
                    ("y_root", ctypes.c_int),
                    ("state", ctypes.c_uint),
                    ("keycode", ctypes.c_uint),
                    ("same_screen", ctypes.c_int),
                ]

            class XEvent(ctypes.Structure):
                _fields_ = [("type", ctypes.c_int), ("pad", ctypes.c_byte * 192)]

            event = XEvent()
            while True:
                x11.XNextEvent(display, ctypes.byref(event))
                if event.type == 2:  # KeyPress
                    kev = ctypes.cast(ctypes.byref(event), ctypes.POINTER(XKeyEvent)).contents
                    if secondary_code != 0 and kev.keycode == secondary_code:
                        engine.save_clip(engine.settings.SecondaryClipDuration)
                    else:
                        engine.save_clip(engine.settings.ClipDuration)
        except Exception:
            pass

    threading.Thread(target=_worker, daemon=True).start()


# =============================================================================
# RAYCAST-INSPIRED TKINTER GRAPHICAL USER INTERFACE
# =============================================================================
def run_gui(settings, engine, ipc=None):
    import tkinter as tk
    import tkinter.font as tkfont
    from tkinter import ttk, filedialog, messagebox

    root = tk.Tk()
    root.title("Rewind - Natychmiastowe powtórki")
    root.geometry("960x640")
    root.minsize(880, 580)

    # 1. Detect best modern antialiased sans-serif font available on this Linux distro
    available_fams = set(tkfont.families(root))
    FONT_FAM = "sans-serif"
    for cand in ["Segoe UI", "Inter", "Roboto", "Ubuntu", "Cantarell", "Liberation Sans", "DejaVu Sans"]:
        if cand in available_fams:
            FONT_FAM = cand
            break

    def fnt(size=10, weight="normal"):
        return (FONT_FAM, size, weight)

    # Register show_window handler for IPC
    if ipc:
        def show_window():
            try:
                root.deiconify()
                root.lift()
                root.focus_force()
            except Exception:
                pass
        ipc.on_show_window = show_window

    # Set icon if available
    icon_path = CONFIG_DIR / "icon.png"
    if icon_path.exists():
        try:
            img = tk.PhotoImage(file=str(icon_path))
            root.iconphoto(True, img)
        except Exception:
            pass

    # Pure Jet Black / Raycast Palette matching Windows ThemePalette
    THEMES = {
        "Dark": {
            "bg": "#0A0A0A",
            "sidebar": "#0F0F0F",
            "sidebar_border": "#1A1A1A",
            "card": "#121214",
            "card_border": "#1E1E24",
            "card_hover": "#17171C",
            "text": "#FFFFFF",
            "text_secondary": "#9696A0",
            "text_muted": "#646470",
            "accent": "#FF5757",
            "accent_hover": "#FF6E6E",
            "pill_bg": "#18181C",
            "pill_border": "#282832",
            "pill_active": "#FF5757",
            "pill_active_bg": "#2B161B",
            "pill_active_fg": "#FFFFFF",
            "status_active": "#10B981",
            "status_active_bg": "#0B2618",
            "status_paused": "#F59E0B"
        },
        "Light": {
            "bg": "#F6F7FA",
            "sidebar": "#EDEFF4",
            "sidebar_border": "#DCDFE6",
            "card": "#FFFFFF",
            "card_border": "#DEE1EA",
            "card_hover": "#F0F2F8",
            "text": "#121218",
            "text_secondary": "#5C5F6C",
            "text_muted": "#888C98",
            "accent": "#FF5757",
            "accent_hover": "#FF6E6E",
            "pill_bg": "#EEF0F6",
            "pill_border": "#D6DAE4",
            "pill_active": "#18181E",
            "pill_active_bg": "#E5E5EB",
            "pill_active_fg": "#FFFFFF",
            "status_active": "#10B981",
            "status_active_bg": "#D1FAE5",
            "status_paused": "#F59E0B"
        }
    }

    C = THEMES.get(settings.Theme, THEMES["Dark"])
    root.configure(bg=C["bg"])

    # Main layout Grid
    root.columnconfigure(0, weight=0, minsize=226)
    root.columnconfigure(1, weight=1)
    root.rowconfigure(0, weight=1)

    # -------------------------------------------------------------------------
    # LEFT SIDEBAR (226px, #0F0F0F)
    # -------------------------------------------------------------------------
    sidebar = tk.Frame(root, bg=C["sidebar"], width=226, highlightthickness=1, highlightbackground=C["sidebar_border"])
    sidebar.grid(row=0, column=0, sticky="nsew")
    sidebar.grid_propagate(False)

    # Brand Header
    brand_frame = tk.Frame(sidebar, bg=C["sidebar"])
    brand_frame.pack(fill="x", padx=18, pady=(20, 16))

    brand_row = tk.Frame(brand_frame, bg=C["sidebar"])
    brand_row.pack(anchor="w")

    tk.Label(
        brand_row,
        text="⏪",
        font=fnt(13, "bold"),
        fg=C["accent"],
        bg=C["sidebar"]
    ).pack(side="left", padx=(0, 6))

    tk.Label(
        brand_row,
        text="Rewind",
        font=fnt(15, "bold"),
        fg=C["text"],
        bg=C["sidebar"]
    ).pack(side="left")

    tk.Label(
        brand_row,
        text="v0.2",
        font=fnt(9, "bold"),
        fg=C["text_secondary"],
        bg=C["pill_bg"],
        padx=6,
        pady=1
    ).pack(side="left", padx=8)

    # Navigation Buttons Container
    nav_frame = tk.Frame(sidebar, bg=C["sidebar"])
    nav_frame.pack(fill="both", expand=True, padx=10, pady=4)

    nav_items = [
        ("🎞️  Powtórki", "clips"),
        ("📹  Wideo i bufor", "video"),
        ("🔊  Dźwięk i mikser", "audio"),
        ("⌨️  Skróty klawiszowe", "hotkeys"),
        ("⚙️  Ustawienia", "settings"),
        ("ℹ️  O programie", "about")
    ]

    nav_buttons = {}
    content_frames = {}
    current_tab = ["clips"]

    # Bottom Status Card in Sidebar
    footer_status_card = tk.Frame(sidebar, bg=C["card"], bd=1, relief="solid", highlightthickness=1, highlightbackground=C["card_border"])
    footer_status_card.pack(fill="x", side="bottom", padx=12, pady=12, ipady=6, ipadx=8)

    status_row = tk.Frame(footer_status_card, bg=C["card"])
    status_row.pack(fill="x", padx=6, pady=(4, 2))

    status_dot = tk.Label(status_row, text="●", font=fnt(10, "bold"), fg=C["status_active"], bg=C["card"])
    status_dot.pack(side="left")

    status_text_lbl = tk.Label(status_row, text="Bufor aktywny", font=fnt(9, "bold"), fg=C["status_active"], bg=C["card"])
    status_text_lbl.pack(side="left", padx=4)

    quick_toggle_btn = tk.Button(
        status_row,
        text="⏸",
        font=fnt(9, "bold"),
        fg=C["text"],
        bg=C["pill_bg"],
        bd=0,
        padx=6,
        pady=1,
        cursor="hand2"
    )
    quick_toggle_btn.pack(side="right")

    status_sub_lbl = tk.Label(
        footer_status_card,
        text=f"{settings.Hotkey} • {settings.ClipDuration}s • {settings.Fps} FPS",
        font=fnt(8),
        fg=C["text_muted"],
        bg=C["card"]
    )
    status_sub_lbl.pack(anchor="w", padx=6, pady=(0, 2))

    def on_quick_toggle():
        if engine.is_recording:
            quick_toggle_btn.config(text="▶", state="disabled")
            threading.Thread(target=lambda: (engine.stop(), root.after(0, lambda: quick_toggle_btn.config(state="normal"))), daemon=True).start()
        else:
            quick_toggle_btn.config(text="⏸", state="disabled")
            threading.Thread(target=lambda: (engine.start(), root.after(0, lambda: quick_toggle_btn.config(state="normal"))), daemon=True).start()

    quick_toggle_btn.config(command=on_quick_toggle)

    def update_status_badge(active, text):
        def _ui():
            if active:
                status_dot.config(fg=C["status_active"])
                status_text_lbl.config(text="Bufor aktywny", fg=C["status_active"])
                quick_toggle_btn.config(text="⏸")
            else:
                status_dot.config(fg=C["status_paused"])
                status_text_lbl.config(text="Bufor zatrzymany", fg=C["text_muted"])
                quick_toggle_btn.config(text="▶")
            status_sub_lbl.config(text=f"{settings.Hotkey} • {settings.ClipDuration}s • {settings.Fps} FPS")
        root.after(0, _ui)

    engine.add_status_listener(update_status_badge)

    # -------------------------------------------------------------------------
    # RIGHT CONTENT AREA
    # -------------------------------------------------------------------------
    right_panel = tk.Frame(root, bg=C["bg"])
    right_panel.grid(row=0, column=1, sticky="nsew")
    right_panel.columnconfigure(0, weight=1)
    right_panel.rowconfigure(0, weight=0)  # Caption / Breadcrumbs
    right_panel.rowconfigure(1, weight=1)  # Views
    right_panel.rowconfigure(2, weight=0)  # Subtle Footer

    # 1. Caption & Breadcrumb Bar
    caption_bar = tk.Frame(right_panel, bg=C["bg"], height=46)
    caption_bar.grid(row=0, column=0, sticky="ew", padx=24, pady=(10, 4))
    caption_bar.grid_propagate(False)

    breadcrumb_lbl = tk.Label(
        caption_bar,
        text="Rewind › Powtórki",
        font=fnt(11, "bold"),
        fg=C["text_secondary"],
        bg=C["bg"]
    )
    breadcrumb_lbl.pack(side="left", pady=8)

    caption_controls = tk.Frame(caption_bar, bg=C["bg"])
    caption_controls.pack(side="right", pady=4)

    min_btn = tk.Button(
        caption_controls,
        text="—",
        font=fnt(10, "bold"),
        fg=C["text_muted"],
        bg=C["bg"],
        bd=0,
        padx=8,
        pady=2,
        cursor="hand2",
        command=lambda: root.iconify()
    )
    min_btn.pack(side="left", padx=2)

    close_btn = tk.Button(
        caption_controls,
        text="✕",
        font=fnt(10, "bold"),
        fg=C["text_muted"],
        bg=C["bg"],
        bd=0,
        padx=8,
        pady=2,
        cursor="hand2",
        command=lambda: root.destroy()
    )
    close_btn.pack(side="left", padx=2)

    # Hover effects for caption buttons
    min_btn.bind("<Enter>", lambda e: min_btn.config(bg=C["pill_bg"], fg=C["text"]))
    min_btn.bind("<Leave>", lambda e: min_btn.config(bg=C["bg"], fg=C["text_muted"]))
    close_btn.bind("<Enter>", lambda e: close_btn.config(bg="#E81123", fg="#FFFFFF"))
    close_btn.bind("<Leave>", lambda e: close_btn.config(bg=C["bg"], fg=C["text_muted"]))

    # 2. Views Container
    views_container = tk.Frame(right_panel, bg=C["bg"])
    views_container.grid(row=1, column=0, sticky="nsew", padx=24, pady=6)
    views_container.columnconfigure(0, weight=1)
    views_container.rowconfigure(0, weight=1)

    # Tab Switching Logic (Smooth & Instantaneous)
    def switch_tab(tab_id):
        current_tab[0] = tab_id
        for t_id, f in content_frames.items():
            if t_id == tab_id:
                f.grid(row=0, column=0, sticky="nsew")
            else:
                f.grid_forget()

        # Update sidebar navigation buttons
        for t_id, btn in nav_buttons.items():
            if t_id == tab_id:
                btn.config(bg="#1E1E24", fg=C["text"], font=fnt(10, "bold"))
            else:
                btn.config(bg=C["sidebar"], fg=C["text_secondary"], font=fnt(10, "normal"))

        # Update breadcrumbs
        titles = {
            "clips": "Powtórki",
            "video": "Wideo i bufor",
            "audio": "Dźwięk i mikser",
            "hotkeys": "Skróty klawiszowe",
            "settings": "Ustawienia",
            "about": "O programie"
        }
        breadcrumb_lbl.config(text=f"Rewind › {titles.get(tab_id, 'Ustawienia')}")

    # Build Navigation Buttons in Sidebar
    for label, tab_id in nav_items:
        b = tk.Button(
            nav_frame,
            text=label,
            font=fnt(10),
            fg=C["text_secondary"],
            bg=C["sidebar"],
            bd=0,
            anchor="w",
            padx=14,
            pady=8,
            cursor="hand2"
        )
        b.pack(fill="x", pady=2)
        b.bind("<Enter>", lambda e, btn=b, tid=tab_id: btn.config(bg="#17171C", fg=C["text"]) if current_tab[0] != tid else None)
        b.bind("<Leave>", lambda e, btn=b, tid=tab_id: btn.config(bg=C["sidebar"], fg=C["text_secondary"]) if current_tab[0] != tid else None)
        b.config(command=lambda tid=tab_id: switch_tab(tid))
        nav_buttons[tab_id] = b

    # Helper: Modern Rounded Card Container
    def create_card(parent, title, subtitle=""):
        card = tk.Frame(parent, bg=C["card"], bd=1, relief="solid", highlightthickness=1, highlightbackground=C["card_border"])
        card.pack(fill="x", pady=6, ipady=10, ipadx=14)
        tk.Label(card, text=title, font=fnt(11, "bold"), fg=C["text"], bg=C["card"]).pack(anchor="w")
        if subtitle:
            tk.Label(card, text=subtitle, font=fnt(9), fg=C["text_muted"], bg=C["card"]).pack(anchor="w", pady=(2, 8))
        return card

    # =========================================================================
    # TAB 1: CLIPS (POWTÓRKI)
    # =========================================================================
    view_clips = tk.Frame(views_container, bg=C["bg"])
    content_frames["clips"] = view_clips

    # Hero Card (Raycast Signature Banner)
    hero_card = tk.Frame(view_clips, bg=C["card"], bd=1, relief="solid", highlightthickness=1, highlightbackground=C["card_border"])
    hero_card.pack(fill="x", pady=(0, 14), ipady=14, ipadx=16)

    hero_hotkey_badge = tk.Label(
        hero_card,
        text=f"[{settings.Hotkey}]",
        font=fnt(9, "bold"),
        fg=C["accent"],
        bg=C["pill_active_bg"],
        padx=8,
        pady=2
    )
    hero_hotkey_badge.pack(anchor="w")

    hero_title = tk.Label(
        hero_card,
        text=f"Zapisz ostatnie {settings.ClipDuration} sekund",
        font=fnt(16, "bold"),
        fg=C["text"],
        bg=C["card"]
    )
    hero_title.pack(anchor="w", pady=(4, 2))

    hero_sub = tk.Label(
        hero_card,
        text=f"Naciśnij {settings.Hotkey} w dowolnej grze lub aplikacji, aby natychmiast utworzyć powtórkę MP4.",
        font=fnt(9),
        fg=C["text_muted"],
        bg=C["card"]
    )
    hero_sub.pack(anchor="w", pady=(0, 10))

    hero_btn = tk.Button(
        hero_card,
        text="⚡ Zapisz powtórkę teraz",
        font=fnt(10, "bold"),
        fg="#FFFFFF",
        bg=C["accent"],
        bd=0,
        padx=18,
        pady=8,
        cursor="hand2"
    )
    hero_btn.pack(anchor="e")

    def on_hero_click():
        hero_btn.config(text="⏳ Zapisywanie...", state="disabled")
        threading.Thread(target=lambda: engine.save_clip(), daemon=True).start()

    hero_btn.config(command=on_hero_click)

    # Toast Notification Banner for Saved Clips
    toast_banner = tk.Frame(view_clips, bg=C["status_active_bg"], bd=1, relief="solid", highlightthickness=1, highlightbackground=C["status_active"])
    toast_lbl = tk.Label(toast_banner, text="✓ Zapisano klip pomyślnie!", font=fnt(9, "bold"), fg=C["status_active"], bg=C["status_active_bg"])
    toast_lbl.pack(side="left", padx=12, pady=6)

    toast_open_btn = tk.Button(toast_banner, text="▶ Odtwórz", font=fnt(8, "bold"), fg="#FFFFFF", bg=C["status_active"], bd=0, padx=8, pady=2, cursor="hand2")
    toast_open_btn.pack(side="right", padx=10, pady=4)

    def show_toast(clip_path):
        def _ui():
            toast_lbl.config(text=f"✓ Zapisano klip: {Path(clip_path).name}")
            toast_open_btn.config(command=lambda: subprocess.Popen(["xdg-open", str(clip_path)]))
            toast_banner.pack(fill="x", pady=(0, 12), before=clips_header_frame)
            root.after(6000, lambda: toast_banner.pack_forget())
        root.after(0, _ui)

    # Clips List Section
    clips_header_frame = tk.Frame(view_clips, bg=C["bg"])
    clips_header_frame.pack(fill="x", pady=(4, 8))

    clips_count_lbl = tk.Label(
        clips_header_frame,
        text="ZAPISANE KLIPY",
        font=fnt(9, "bold"),
        fg=C["text_muted"],
        bg=C["bg"]
    )
    clips_count_lbl.pack(side="left")

    open_folder_btn = tk.Button(
        clips_header_frame,
        text="📂 Otwórz folder",
        font=fnt(9),
        fg=C["text_secondary"],
        bg=C["pill_bg"],
        bd=0,
        padx=10,
        pady=3,
        cursor="hand2",
        command=lambda: subprocess.Popen(["xdg-open", settings.ClipsFolder])
    )
    open_folder_btn.pack(side="right")

    clips_scroll_container = tk.Frame(view_clips, bg=C["bg"])
    clips_scroll_container.pack(fill="both", expand=True)

    selected_clip_path = [None]

    def refresh_clips_list():
        for w in clips_scroll_container.winfo_children():
            w.destroy()

        clips_dir = Path(settings.ClipsFolder)
        if not clips_dir.exists():
            return

        mp4_files = sorted(clips_dir.glob("Clip_*.mp4"), key=lambda f: f.stat().st_mtime, reverse=True)
        clips_count_lbl.config(text=f"ZAPISANE KLIPY ({len(mp4_files)})")

        if not mp4_files:
            empty_lbl = tk.Label(
                clips_scroll_container,
                text=f"Brak zapisanych powtórek.\nNaciśnij {settings.Hotkey} podczas gry, aby utworzyć pierwszy klip!",
                font=fnt(10),
                fg=C["text_muted"],
                bg=C["bg"],
                pady=40
            )
            empty_lbl.pack()
            return

        for f in mp4_files[:10]:
            card = tk.Frame(clips_scroll_container, bg=C["card"], bd=1, relief="solid", highlightthickness=1, highlightbackground=C["card_border"])
            card.pack(fill="x", pady=3, ipady=6, ipadx=10)

            size_mb = f.stat().st_size / (1024 * 1024)
            date_str = time.strftime("%Y-%m-%d %H:%M", time.localtime(f.stat().st_mtime))

            left_box = tk.Frame(card, bg=C["card"])
            left_box.pack(side="left", fill="both", expand=True)

            name_lbl = tk.Label(
                left_box,
                text=f"🎬  {f.name}",
                font=fnt(10, "bold"),
                fg=C["text"],
                bg=C["card"],
                anchor="w"
            )
            name_lbl.pack(anchor="w")

            info_lbl = tk.Label(
                left_box,
                text=f"{date_str}  •  {size_mb:.1f} MB  (kliknij dwukrotnie, aby odtworzyć)",
                font=fnt(8),
                fg=C["text_muted"],
                bg=C["card"],
                anchor="w"
            )
            info_lbl.pack(anchor="w")

            # Actions
            btn_box = tk.Frame(card, bg=C["card"])
            btn_box.pack(side="right")

            play_b = tk.Button(
                btn_box,
                text="▶ Odtwórz",
                font=fnt(8, "bold"),
                fg=C["text"],
                bg=C["pill_bg"],
                bd=0,
                padx=8,
                pady=3,
                cursor="hand2",
                command=lambda p=f: subprocess.Popen(["xdg-open", str(p)])
            )
            play_b.pack(side="left", padx=2)

            trim_b = tk.Button(
                btn_box,
                text="✂️ Przytnij",
                font=fnt(8),
                fg=C["text_secondary"],
                bg=C["pill_bg"],
                bd=0,
                padx=8,
                pady=3,
                cursor="hand2",
                command=lambda p=f: show_trim_dialog(p)
            )
            trim_b.pack(side="left", padx=2)

            del_b = tk.Button(
                btn_box,
                text="🗑️",
                font=fnt(8),
                fg="#EF4444",
                bg=C["pill_bg"],
                bd=0,
                padx=6,
                pady=3,
                cursor="hand2",
                command=lambda p=f: delete_clip(p)
            )
            del_b.pack(side="left", padx=2)

            # Double-click to play (Windows parity)
            for widget in [card, left_box, name_lbl, info_lbl]:
                widget.bind("<Double-Button-1>", lambda e, p=f: subprocess.Popen(["xdg-open", str(p)]))
                widget.bind("<Button-1>", lambda e, p=f: select_clip(p))

    def select_clip(p):
        selected_clip_path[0] = p

    def delete_clip(file_path):
        if messagebox.askyesno("Usuń klip", f"Czy na pewno chcesz usunąć powtórkę {file_path.name}?"):
            try:
                file_path.unlink()
                refresh_clips_list()
            except Exception as e:
                messagebox.showerror("Błąd", str(e))

    # Delete key shortcut (Windows parity)
    root.bind("<Delete>", lambda e: delete_clip(selected_clip_path[0]) if selected_clip_path[0] else None)

    # Listeners for Saving status
    engine.add_clip_saving_listener(lambda dur: root.after(0, lambda: hero_btn.config(text="⏳ Zapisywanie...", state="disabled")))
    engine.add_clip_saved_done_listener(lambda: root.after(0, lambda: hero_btn.config(text="⚡ Zapisz powtórkę teraz", state="normal")))
    engine.add_clip_saved_listener(lambda path, dur: (show_toast(path), root.after(0, refresh_clips_list)))

    # Modal Trim Dialog (Non-blocking & Asynchronous)
    def show_trim_dialog(file_path):
        dlg = tk.Toplevel(root)
        dlg.title("Przycinanie powtórki")
        dlg.geometry("440x230")
        dlg.configure(bg=C["card"])
        dlg.resizable(False, False)
        dlg.transient(root)
        dlg.grab_set()

        tk.Label(dlg, text="✂️ Szybkie przycinanie powtórki", font=fnt(12, "bold"), fg=C["text"], bg=C["card"]).pack(anchor="w", padx=20, pady=(18, 2))
        tk.Label(dlg, text=file_path.name, font=fnt(9), fg=C["text_muted"], bg=C["card"]).pack(anchor="w", padx=20, pady=(0, 14))

        fields = tk.Frame(dlg, bg=C["card"])
        fields.pack(fill="x", padx=20, pady=(0, 16))

        s_col = tk.Frame(fields, bg=C["card"])
        s_col.pack(side="left", fill="x", expand=True, padx=(0, 8))
        tk.Label(s_col, text="Początek (sekundy):", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w")
        s_entry = tk.Entry(s_col, font=fnt(11), bg=C["pill_bg"], fg=C["text"], bd=1, relief="solid")
        s_entry.insert(0, "0")
        s_entry.pack(fill="x", pady=4)

        e_col = tk.Frame(fields, bg=C["card"])
        e_col.pack(side="left", fill="x", expand=True, padx=(8, 0))
        tk.Label(e_col, text="Koniec (sekundy):", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w")
        e_entry = tk.Entry(e_col, font=fnt(11), bg=C["pill_bg"], fg=C["text"], bd=1, relief="solid")
        e_entry.insert(0, "15")
        e_entry.pack(fill="x", pady=4)

        btn_row = tk.Frame(dlg, bg=C["card"])
        btn_row.pack(fill="x", padx=20, pady=(4, 16))

        def on_save_trim():
            try:
                start_val = float(s_entry.get().replace(",", "."))
                end_val = float(e_entry.get().replace(",", "."))
            except ValueError:
                messagebox.showerror("Błąd", "Wprowadź prawidłowe liczby sekund.")
                return

            if start_val < 0 or end_val <= start_val:
                messagebox.showerror("Błąd", "Czas końca musi być większy niż czas początku (>= 0).")
                return

            save_trim_btn.config(text="⏳ Przycinanie...", state="disabled")

            def _worker():
                success, res = engine.trim_clip(file_path, start_val, end_val)
                def _done():
                    if dlg.winfo_exists():
                        dlg.destroy()
                        if success:
                            refresh_clips_list()
                            show_toast(res)
                        else:
                            messagebox.showerror("Błąd", res)
                root.after(0, _done)

            threading.Thread(target=_worker, daemon=True).start()

        save_trim_btn = tk.Button(btn_row, text="⚡ Zapisz przycięty klip", font=fnt(9, "bold"), fg="#FFFFFF", bg=C["accent"], bd=0, padx=14, pady=6, cursor="hand2", command=on_save_trim)
        save_trim_btn.pack(side="right")

        cancel_trim_btn = tk.Button(btn_row, text="Anuluj", font=fnt(9), fg=C["text_muted"], bg=C["pill_bg"], bd=0, padx=12, pady=6, cursor="hand2", command=dlg.destroy)
        cancel_trim_btn.pack(side="right", padx=8)

    refresh_clips_list()

    # =========================================================================
    # TAB 2: VIDEO & BUFFER SETTINGS (WIDEO I BUFOR)
    # =========================================================================
    view_video = tk.Frame(views_container, bg=C["bg"])
    content_frames["video"] = view_video

    # Helper for in-place pill highlights (0 ms latency, no redraw!)
    def setup_pill_group(parent_frame, options, active_val, on_select):
        btns = {}
        for opt in options:
            label = f"{opt}s" if isinstance(opt, int) and opt < 300 and "FPS" not in str(opt) else str(opt)
            if "FPS" not in label and "Mbps" not in label and not label.endswith("s") and not label.endswith("GB") and not label.endswith("dni"):
                label = f"{opt} FPS" if "FPS" in parent_frame.winfo_name() else label

            is_active = (opt == active_val)
            b = tk.Button(
                parent_frame,
                text=label,
                font=fnt(9, "bold" if is_active else "normal"),
                fg="#FFFFFF" if is_active else C["text_secondary"],
                bg=C["pill_active"] if is_active else C["pill_bg"],
                bd=0,
                padx=12,
                pady=5,
                cursor="hand2"
            )
            b.pack(side="left", padx=3)
            btns[opt] = b

        def _select(val):
            for k, btn in btns.items():
                if k == val:
                    btn.config(bg=C["pill_active"], fg="#FFFFFF", font=fnt(9, "bold"))
                else:
                    btn.config(bg=C["pill_bg"], fg=C["text_secondary"], font=fnt(9, "normal"))
            on_select(val)

        for k, btn in btns.items():
            btn.config(command=lambda v=k: _select(v))

        return _select

    # Duration Card
    c_dur = create_card(view_video, "Długość bufora wideo", "Czas rejestrowany wstecz w pamięci podręcznej")
    dur_frame = tk.Frame(c_dur, bg=C["card"])
    dur_frame.pack(anchor="w")

    def on_set_duration(d):
        settings.ClipDuration = d
        settings.save()
        hero_title.config(text=f"Zapisz ostatnie {d} sekund")
        status_sub_lbl.config(text=f"{settings.Hotkey} • {d}s • {settings.Fps} FPS")

    setup_pill_group(dur_frame, [15, 30, 60, 120], settings.ClipDuration, on_set_duration)

    # FPS Card
    c_fps = create_card(view_video, "Płynność nagrywania (FPS)", "Liczba klatek na sekundę dla bufora wideo")
    fps_frame = tk.Frame(c_fps, bg=C["card"], name="fps_frame")
    fps_frame.pack(anchor="w")

    def on_set_fps(f):
        settings.Fps = f
        settings.save()
        status_sub_lbl.config(text=f"{settings.Hotkey} • {settings.ClipDuration}s • {f} FPS")
        threading.Thread(target=lambda: (engine.stop(), engine.start()), daemon=True).start()

    setup_pill_group(fps_frame, [30, 60, 120, 144], settings.Fps, on_set_fps)

    # Bitrate Card
    c_bit = create_card(view_video, "Jakość wideo (Bitrate)", "Przepływność strumienia wideo")
    bit_frame = tk.Frame(c_bit, bg=C["card"])
    bit_frame.pack(anchor="w")

    def on_set_bitrate(b):
        settings.Bitrate = b
        settings.save()
        threading.Thread(target=lambda: (engine.stop(), engine.start()), daemon=True).start()

    setup_pill_group(bit_frame, ["8M", "12M", "16M"], settings.Bitrate, on_set_bitrate)

    # Folder Location Card
    c_folder = create_card(view_video, "Lokalizacja zapisu powtórek", "Katalog, w którym zapisywane są gotowe pliki MP4")
    f_row = tk.Frame(c_folder, bg=C["card"])
    f_row.pack(fill="x", pady=(2, 4))

    folder_lbl = tk.Label(f_row, text=settings.ClipsFolder, font=fnt(9), fg=C["text_secondary"], bg=C["pill_bg"], padx=10, pady=5)
    folder_lbl.pack(side="left", fill="x", expand=True)

    def on_choose_folder():
        new_d = filedialog.askdirectory(initialdir=settings.ClipsFolder)
        if new_d:
            settings.ClipsFolder = new_d
            settings.save()
            folder_lbl.config(text=new_d)
            refresh_clips_list()

    change_f_btn = tk.Button(f_row, text="Zmień folder", font=fnt(9, "bold"), fg=C["text"], bg=C["pill_bg"], bd=0, padx=12, pady=5, cursor="hand2", command=on_choose_folder)
    change_f_btn.pack(side="left", padx=8)

    # =========================================================================
    # TAB 3: AUDIO SETTINGS (DŹWIĘK I MIKSER)
    # =========================================================================
    view_audio = tk.Frame(views_container, bg=C["bg"])
    content_frames["audio"] = view_audio

    # System Audio Volume
    c_sysvol = create_card(view_audio, "Głośność dźwięku z komputera", "Dźwięki z gier, Discorda i systemu (PipeWire / PulseAudio)")
    sys_val_lbl = tk.Label(c_sysvol, text=f"{settings.SystemVolumePercent}%", font=fnt(10, "bold"), fg=C["accent"], bg=C["card"])
    sys_val_lbl.pack(anchor="w")

    sys_slider = tk.Scale(
        c_sysvol,
        from_=0,
        to=1000 if settings.AdvancedMode else 150,
        orient="horizontal",
        showvalue=False,
        bg=C["card"],
        fg=C["accent"],
        troughcolor=C["pill_bg"],
        activebackground=C["accent_hover"],
        highlightthickness=0,
        bd=0,
        length=420
    )
    sys_slider.set(settings.SystemVolumePercent)
    sys_slider.pack(anchor="w", pady=(4, 6))

    def on_sys_change(v):
        val = int(v)
        settings.SystemVolumePercent = val
        settings.save()
        sys_val_lbl.config(text=f"{val}%")

    sys_slider.config(command=on_sys_change)

    # Microphone Volume
    c_micvol = create_card(view_audio, "Głośność mikrofonu", "Rejestrowanie Twojego głosu podczas rozgrywki")
    mic_val_lbl = tk.Label(c_micvol, text=f"{settings.MicVolumePercent}%", font=fnt(10, "bold"), fg=C["accent"], bg=C["card"])
    mic_val_lbl.pack(anchor="w")

    mic_slider = tk.Scale(
        c_micvol,
        from_=0,
        to=1000 if settings.AdvancedMode else 150,
        orient="horizontal",
        showvalue=False,
        bg=C["card"],
        fg=C["accent"],
        troughcolor=C["pill_bg"],
        activebackground=C["accent_hover"],
        highlightthickness=0,
        bd=0,
        length=420
    )
    mic_slider.set(settings.MicVolumePercent)
    mic_slider.pack(anchor="w", pady=(4, 6))

    def on_mic_change(v):
        val = int(v)
        settings.MicVolumePercent = val
        settings.save()
        mic_val_lbl.config(text=f"{val}%")

    mic_slider.config(command=on_mic_change)

    # Multi-track Audio Card
    c_multi = create_card(view_audio, "Osobne ścieżki audio w pliku MP4 (Multi-track)", "Zapisuj dźwięk gry i mikrofon na niezależnych ścieżkach do montażu w Premiere / DaVinci")
    multi_var = tk.BooleanVar(value=settings.SeparateAudioTracks)

    def on_toggle_multi():
        settings.SeparateAudioTracks = multi_var.get()
        settings.save()
        threading.Thread(target=lambda: (engine.stop(), engine.start()), daemon=True).start()

    multi_chk = tk.Checkbutton(
        c_multi,
        text="Włącz Multi-track (Ścieżka 1: Dźwięk gry, Ścieżka 2: Mikrofon)",
        variable=multi_var,
        font=fnt(10),
        bg=C["card"],
        fg=C["text"],
        selectcolor=C["pill_bg"],
        activebackground=C["card"],
        activeforeground=C["text"],
        command=on_toggle_multi
    )
    multi_chk.pack(anchor="w")

    # Sound Notification Card
    c_sound = create_card(view_audio, "Dźwięk potwierdzenia zapisu", "Odtwarzaj krótki dźwięk po pomyślnym zapisaniu powtórki")
    tk.Button(
        c_sound,
        text="🔔 Odtwórz dźwięk testowy",
        font=fnt(9),
        fg=C["text"],
        bg=C["pill_bg"],
        bd=0,
        padx=12,
        pady=5,
        cursor="hand2",
        command=play_sound
    ).pack(anchor="w")

    # =========================================================================
    # TAB 4: HOTKEYS (SKRÓTY KLAWISZOWE)
    # =========================================================================
    view_hotkeys = tk.Frame(views_container, bg=C["bg"])
    content_frames["hotkeys"] = view_hotkeys

    # Primary Hotkey
    c_hk1 = create_card(view_hotkeys, "Główny skrót powtórki", "Klawisz wyzwalający natychmiastowy zapis powtórki")
    hk1_frame = tk.Frame(c_hk1, bg=C["card"])
    hk1_frame.pack(anchor="w")

    def on_set_hk(k):
        settings.Hotkey = k
        settings.save()
        hero_hotkey_badge.config(text=f"[{k}]")
        status_sub_lbl.config(text=f"{k} • {settings.ClipDuration}s • {settings.Fps} FPS")

    setup_pill_group(hk1_frame, ["F7", "F8", "F9", "F10", "F11", "F12"], settings.Hotkey, on_set_hk)

    # Secondary Hotkey (Long Clips)
    c_hk2 = create_card(view_hotkeys, "Drugi skrót (Długie powtórki)", "Niezależny skrót do rejestrowania dłuższych fragmentów (np. całej rundy gry)")
    sec_var = tk.BooleanVar(value=settings.SecondaryHotkeyEnabled)

    def on_toggle_sec():
        settings.SecondaryHotkeyEnabled = sec_var.get()
        settings.save()

    sec_chk = tk.Checkbutton(
        c_hk2,
        text="Włącz drugi skrót klawiszowy",
        variable=sec_var,
        font=fnt(10),
        bg=C["card"],
        fg=C["text"],
        selectcolor=C["pill_bg"],
        activebackground=C["card"],
        activeforeground=C["text"],
        command=on_toggle_sec
    )
    sec_chk.pack(anchor="w", pady=(0, 6))

    hk2_frame = tk.Frame(c_hk2, bg=C["card"])
    hk2_frame.pack(anchor="w", pady=(0, 6))

    def on_set_sec_hk(k):
        settings.SecondaryHotkey = k
        settings.save()

    setup_pill_group(hk2_frame, ["F7", "F8", "F9", "F10", "F11", "F12"], settings.SecondaryHotkey, on_set_sec_hk)

    tk.Label(c_hk2, text="Czas trwania drugiej powtórki:", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=(4, 2))
    sec_dur_frame = tk.Frame(c_hk2, bg=C["card"])
    sec_dur_frame.pack(anchor="w")

    def on_set_sec_dur(d):
        settings.SecondaryClipDuration = d
        settings.save()

    setup_pill_group(sec_dur_frame, [60, 120, 180, 300], settings.SecondaryClipDuration, on_set_sec_dur)

    # Wayland Hint Card
    create_card(
        view_hotkeys,
        "Środowiska Wayland (GNOME / KDE Plasma / Hyprland / Sway)",
        "Dla 100% niezawodności w grach natywnych Wayland, przypisz skrót w Ustawieniach Systemowych -> Klawiatura do polecenia:\n• rewind --save (główny klip)\n• rewind --save-long (długi klip)"
    )

    # =========================================================================
    # TAB 5: SETTINGS (USTAWIENIA)
    # =========================================================================
    view_settings = tk.Frame(views_container, bg=C["bg"])
    content_frames["settings"] = view_settings

    # Theme Selector
    c_theme = create_card(view_settings, "Motyw interfejsu", "Wybierz schemat kolorystyczny aplikacji")
    theme_frame = tk.Frame(c_theme, bg=C["card"])
    theme_frame.pack(anchor="w")

    def on_set_theme(th):
        settings.Theme = th
        settings.save()
        messagebox.showinfo("Rewind", "Zmieniono motyw. Zostanie w pełni zastosowany po ponownym uruchomieniu programu.")

    setup_pill_group(theme_frame, ["Dark", "Light"], settings.Theme, on_set_theme)

    # Language Selector
    c_lang = create_card(view_settings, "Język / Language", "Wybierz język interfejsu programu")
    lang_frame = tk.Frame(c_lang, bg=C["card"])
    lang_frame.pack(anchor="w")

    def on_set_lang(l_code):
        settings.Language = l_code
        settings.save()

    lang_labels = {"pl": "🇵🇱 Polski", "en": "🇬🇧 English", "de": "🇩🇪 Deutsch", "es": "🇪🇸 Español", "fr": "🇫🇷 Français", "ua": "🇺🇦 Українська"}
    for l_c, l_name in lang_labels.items():
        is_l_act = (settings.Language == l_c)
        l_btn = tk.Button(
            lang_frame,
            text=l_name,
            font=fnt(9, "bold" if is_l_act else "normal"),
            fg="#FFFFFF" if is_l_act else C["text_secondary"],
            bg=C["pill_active"] if is_l_act else C["pill_bg"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda c=l_c: on_set_lang(c)
        )
        l_btn.pack(side="left", padx=2)

    # Advanced Mode Toggle
    c_adv = create_card(view_settings, "Tryb zaawansowany (Creator Mode)", "Odblokowuje suwaki wzmocnienia głośności do 1000% oraz zaawansowane parametry")
    adv_var = tk.BooleanVar(value=settings.AdvancedMode)

    def on_toggle_adv():
        settings.AdvancedMode = adv_var.get()
        settings.save()
        sys_slider.config(to=1000 if settings.AdvancedMode else 150)
        mic_slider.config(to=1000 if settings.AdvancedMode else 150)

    adv_chk = tk.Checkbutton(
        c_adv,
        text="Włącz tryb zaawansowany (do 1000% głośności)",
        variable=adv_var,
        font=fnt(10),
        bg=C["card"],
        fg=C["text"],
        selectcolor=C["pill_bg"],
        activebackground=C["card"],
        activeforeground=C["text"],
        command=on_toggle_adv
    )
    adv_chk.pack(anchor="w")

    # Storage Quota Management
    c_stor = create_card(view_settings, "Zarządzanie przestrzenią dyskową", "Automatyczne pilnowanie limitu miejsca i usuwanie starych klipów")
    stor_var = tk.BooleanVar(value=settings.StorageLimitEnabled)

    def on_toggle_stor():
        settings.StorageLimitEnabled = stor_var.get()
        settings.save()
        if settings.StorageLimitEnabled:
            engine.enforce_storage_limits()
            refresh_clips_list()

    stor_chk = tk.Checkbutton(
        c_stor,
        text="Włącz automatyczne zarządzanie miejscem na dysku",
        variable=stor_var,
        font=fnt(10),
        bg=C["card"],
        fg=C["text"],
        selectcolor=C["pill_bg"],
        activebackground=C["card"],
        activeforeground=C["text"],
        command=on_toggle_stor
    )
    stor_chk.pack(anchor="w", pady=(0, 6))

    tk.Label(c_stor, text="Limit rozmiaru folderu powtórek:", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=(4, 2))
    gb_frame = tk.Frame(c_stor, bg=C["card"])
    gb_frame.pack(anchor="w", pady=(0, 6))

    def on_set_gb(g):
        settings.MaxStorageLimitGB = g
        settings.save()
        if settings.StorageLimitEnabled:
            engine.enforce_storage_limits()
            refresh_clips_list()

    setup_pill_group(gb_frame, [10, 25, 50, 100], settings.MaxStorageLimitGB, on_set_gb)

    # =========================================================================
    # TAB 6: ABOUT (O PROGRAMIE)
    # =========================================================================
    view_about = tk.Frame(views_container, bg=C["bg"])
    content_frames["about"] = view_about

    c_info = create_card(view_about, f"Rewind {APP_VERSION} Linux Edition", "Wydajne narzędzie powtórek wideo dla systemów Linux (X11 & Wayland)")
    tk.Label(c_info, text=f"• Sprzętowy enkoder: {engine.encoder.upper()}", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=2)
    tk.Label(c_info, text="• Format bufora: Ring buffer MPEG-TS (2-sekundowe bloki w pamięci RAM/tmp)", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=2)
    tk.Label(c_info, text="• Zużycie zasobów: ~40 MB RAM, zerowy narzut CPU przy akceleracji GPU", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=2)
    tk.Label(c_info, text="• Dźwięk: PipeWire / PulseAudio z mikserem do 1000% wzmocnienia", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=2)
    tk.Label(c_info, text="• Projekt: Open Source (GitHub: qavi999/Rewind)", font=fnt(9), fg=C["text_secondary"], bg=C["card"]).pack(anchor="w", pady=2)

    # 3. Subtle Footer Bar
    footer_bar = tk.Frame(right_panel, bg=C["bg"], height=28)
    footer_bar.grid(row=2, column=0, sticky="ew", padx=24, pady=(2, 8))

    tk.Label(
        footer_bar,
        text=f"⚡ {engine.encoder.upper()} Hardware Acceleration • PipeWire High Definition Audio",
        font=fnt(8),
        fg=C["text_muted"],
        bg=C["bg"]
    ).pack(side="left")

    tk.Label(
        footer_bar,
        text="Esc - Zminimalizuj",
        font=fnt(8),
        fg=C["text_muted"],
        bg=C["bg"]
    ).pack(side="right")

    # Global Key Bindings
    root.bind("<Escape>", lambda e: root.iconify())

    # Switch to initial tab
    switch_tab("clips")

    # Start buffer on launch if configured
    if settings.BufferActive and not engine.is_recording:
        threading.Thread(target=engine.start, daemon=True).start()

    def on_closing():
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()


# =============================================================================
# MAIN ENTRY POINT
# =============================================================================
def main():
    parser = argparse.ArgumentParser(description="Rewind Linux instant replay clipping tool")
    parser.add_argument("--save", action="store_true", help="Natychmiast zapisz powtórkę z bufora")
    parser.add_argument("--save-long", action="store_true", help="Zapisz dłuższą powtórkę z bufora (drugi skrót)")
    parser.add_argument("--toggle", action="store_true", help="Wstrzymaj lub wznów działanie bufora")
    parser.add_argument("--status", action="store_true", help="Wyświetl stan bufora")
    parser.add_argument("--daemon", action="store_true", help="Uruchom w tle bez interfejsu graficznego")
    parser.add_argument("--minimized", action="store_true", help="Uruchom zminimalizowany")
    args = parser.parse_args()

    # Handle CLI client commands
    if args.save:
        res = send_ipc_command("SAVE")
        if res and res.startswith("OK"):
            print("[Rewind] Polecenie zapisu wysłane do działającej instancji.")
            sys.exit(0)
        else:
            print("[Rewind] Błąd: Bufor Rewind nie jest aktywny w tle!", file=sys.stderr)
            print("[Rewind] Uruchom program (np. 'rewind' lub 'rewind --daemon'), aby rozpocząć rejestrację.", file=sys.stderr)
            sys.exit(1)

    if args.save_long:
        res = send_ipc_command("SAVE_LONG")
        if res and res.startswith("OK"):
            print("[Rewind] Polecenie zapisu dłuższego klipu wysłane do działającej instancji.")
            sys.exit(0)
        else:
            print("[Rewind] Błąd: Bufor Rewind nie jest aktywny w tle!", file=sys.stderr)
            print("[Rewind] Uruchom program (np. 'rewind' lub 'rewind --daemon'), aby rozpocząć rejestrację.", file=sys.stderr)
            sys.exit(1)

    if args.toggle:
        res = send_ipc_command("TOGGLE")
        if res:
            print(f"[Rewind] Stan bufora przełączony: {res}")
            sys.exit(0)
        else:
            print("[Rewind] Bufor jest nieaktywny (brak działającej instancji).", file=sys.stderr)
            sys.exit(1)

    if args.status:
        res = send_ipc_command("STATUS")
        if res:
            print(f"[Rewind] {res}")
        else:
            print("[Rewind] Bufor jest nieaktywny (brak działającej instancji).")
        sys.exit(0)

    # Check if another instance is already running
    show_resp = send_ipc_command("SHOW")
    if show_resp == "OK:SHOWN":
        print("[Rewind] Istniejąca instancja programu została wywołana na pierwszy plan.")
        sys.exit(0)
    elif show_resp == "ERR:HEADLESS":
        print("[Rewind] Wykryto działający bufor w tle (daemon). Zatrzymywanie daemona i uruchamianie GUI...")
        send_ipc_command("STOP_FOR_GUI")
        time.sleep(0.5)

    # Initialize assets and settings
    ensure_assets()
    settings = AppSettings.load()
    engine = BufferEngine(settings)

    # Start IPC server
    ipc = IpcServer(engine)
    ipc.start()

    # Global Hotkey listener for X11
    start_x11_hotkey_listener(engine)

    # Handle SIGUSR1 (allows `killall -USR1 rewind` or `pkill -USR1 -f rewind` to save clip)
    signal.signal(signal.SIGUSR1, lambda sig, frame: engine.save_clip())

    if args.daemon:
        engine.start()
        print("[Rewind] Serwis bufora uruchomiony w tle (tryb daemon).")
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            engine.stop()
            sys.exit(0)

    # Try running GUI, fallback to daemon if DISPLAY is missing
    try:
        run_gui(settings, engine, ipc)
    except Exception as e:
        print(f"[Rewind] Środowisko graficzne niedostępne ({e}). Uruchamianie w trybie daemon...", file=sys.stderr)
        engine.start()
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            engine.stop()
            sys.exit(0)


if __name__ == "__main__":
    main()
