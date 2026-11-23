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
BUFFER_DIR = Path(tempfile.gettempdir()) / "rewind_buffer"
DEFAULT_CLIPS_DIR = Path.home() / "Videos" / "Rewind"
SOCKET_FILE = Path(tempfile.gettempdir()) / f"rewind_{os.getuid()}.sock"
LOCK_FILE = Path(tempfile.gettempdir()) / f"rewind_{os.getuid()}.lock"


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
        self.lock = threading.Lock()
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

            # Video input (X11 / XWayland)
            display = os.environ.get("DISPLAY", ":0.0")
            cmd.extend([
                "-f", "x11grab",
                "-draw_mouse", str(draw_mouse),
                "-video_size", f"{w}x{h}",
                "-framerate", str(fps),
                "-i", f"{display}+{ox},{oy}"
            ])

            # Audio inputs
            has_pulse = shutil.which("pactl") or shutil.which("pulseaudio") or shutil.which("pipewire")
            use_audio = bool(has_pulse)

            if use_audio:
                cmd.extend(["-f", "pulse", "-i", sys_mon])
                cmd.extend(["-f", "pulse", "-i", mic_src])
                if self.settings.SeparateAudioTracks:
                    cmd.extend([
                        "-filter_complex",
                        f"[1:a]volume={sys_vol:.2f}[a_sys];[2:a]volume={mic_vol:.2f}[a_mic]",
                        "-map", "0:v",
                        "-map", "[a_sys]",
                        "-map", "[a_mic]",
                        "-metadata:s:a:0", "title=Game Audio",
                        "-metadata:s:a:1", "title=Microphone"
                    ])
                else:
                    cmd.extend([
                        "-filter_complex",
                        f"[1:a]volume={sys_vol:.2f}[a0];[2:a]volume={mic_vol:.2f}[a1];[a0][a1]amix=inputs=2:duration=first:dropout_transition=0[aout]",
                        "-map", "0:v",
                        "-map", "[aout]"
                    ])
            else:
                cmd.extend(["-map", "0:v"])

            # Video encoder settings
            if self.encoder == "h264_nvenc":
                cmd.extend([
                    "-c:v", "h264_nvenc",
                    "-preset", "p1",
                    "-tune", "ull",
                    "-b:v", bitrate,
                    "-maxrate", bitrate,
                    "-bufsize", "8M"
                ])
            elif self.encoder == "h264_vaapi":
                cmd.extend([
                    "-vaapi_device", "/dev/dri/renderD128",
                    "-vf", "format=nv12,hwupload",
                    "-c:v", "h264_vaapi",
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

            # Audio codec & segmenting
            if use_audio:
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

            try:
                self.process = subprocess.Popen(
                    cmd,
                    stdin=subprocess.PIPE,
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL
                )
                self.record_start_time = time.time()
                self.is_recording = True
                enc_name = self.encoder.replace("h264_", "").upper()
                self.notify_status(True, f"Bufor aktywny ({enc_name} + Audio)")
            except Exception as e:
                self.is_recording = False
                self.notify_status(False, f"Błąd uruchamiania bufora: {e}")

    def stop(self):
        with self.lock:
            self.is_recording = False
            if self.process:
                try:
                    self.process.terminate()
                    self.process.wait(timeout=1.0)
                except Exception:
                    try:
                        self.process.kill()
                    except Exception:
                        pass
                self.process = None
            self.clean_buffer_dir()
            self.notify_status(False, "Bufor zatrzymany")

    def _watchdog_loop(self):
        while True:
            time.sleep(2.5)
            try:
                with self.lock:
                    if not self.settings.BufferActive or self.is_saving:
                        continue

                    # Revive if dead
                    if self.is_recording:
                        if self.process is None or self.process.poll() is not None:
                            self.stop()
                            self.start()
                            continue

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
        files = sorted(
            [f for f in BUFFER_DIR.glob("seg_*.ts") if f.stat().st_size > 2048],
            key=lambda f: f.name
        )

        if not files:
            show_notification("Rewind", "Brak nagranych danych w buforze (odczekaj kilka sekund).")
            return

        newest = max(files, key=lambda f: f.stat().st_mtime)
        if time.time() - newest.stat().st_mtime > 8.0:
            show_notification("Rewind", "Bufor nie zawierał świeżych klatek. Trwa restart bufora.")
            self.stop()
            self.start()
            return

        target_dur = custom_duration if custom_duration > 0 else self.settings.ClipDuration
        needed_count = math.ceil(target_dur / 2.0) + 1
        chosen = files[-needed_count:]

        # Create concat list
        concat_file = BUFFER_DIR / f"concat_{int(time.time()*1000)}.txt"
        with open(concat_file, "w", encoding="utf-8") as f:
            for seg in chosen:
                f.write(f"file '{seg.resolve()}'\n")

        clips_dir = Path(self.settings.ClipsFolder)
        clips_dir.mkdir(parents=True, exist_ok=True)

        timestamp = time.strftime("%Y-%m-%d_%H-%M-%S")
        output_clip = clips_dir / f"Clip_{timestamp}.mp4"

        cmd = [
            "ffmpeg", "-y", "-nostats", "-loglevel", "error",
            "-f", "concat", "-safe", "0",
            "-i", str(concat_file),
            "-c", "copy",
            "-fflags", "+genpts",
            "-avoid_negative_ts", "make_zero",
            str(output_clip)
        ]

        subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=12)

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
            if SOCKET_FILE.exists():
                SOCKET_FILE.unlink()
        except Exception:
            pass

        self.sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.sock.bind(str(SOCKET_FILE))
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
    """Listen for global hotkeys on X11 root window via XGrabKey."""
    def _worker():
        try:
            x11 = ctypes.cdll.LoadLibrary("libX11.so.6")
            display = x11.XOpenDisplay(None)
            if not display:
                return

            root = x11.XDefaultRootWindow(display)

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
def run_gui(settings, engine):
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox

    root = tk.Tk()
    root.title("Rewind - Natychmiastowe powtórki")
    root.geometry("960x640")
    root.minsize(880, 580)

    # Set icon if available
    icon_path = CONFIG_DIR / "icon.png"
    if icon_path.exists():
        try:
            img = tk.PhotoImage(file=str(icon_path))
            root.iconphoto(True, img)
        except Exception:
            pass

    # Styling colors
    THEMES = {
        "Dark": {
            "bg": "#0D0D11",
            "sidebar": "#121218",
            "card": "#181822",
            "card_border": "#252535",
            "text": "#F3F3F6",
            "text_muted": "#8E8E9E",
            "accent": "#FF5757",
            "pill_bg": "#22222E",
            "pill_active": "#FF5757",
            "status_active": "#10B981",
            "status_text": "#D1FAE5"
        },
        "Light": {
            "bg": "#F5F5F7",
            "sidebar": "#EBECEF",
            "card": "#FFFFFF",
            "card_border": "#E2E2E8",
            "text": "#1A1A24",
            "text_muted": "#686878",
            "accent": "#FF5757",
            "pill_bg": "#E5E5EB",
            "pill_active": "#18181E",
            "status_active": "#10B981",
            "status_text": "#065F46"
        }
    }

    current_theme = THEMES.get(settings.Theme, THEMES["Dark"])
    root.configure(bg=current_theme["bg"])

    # Main layout Grid
    root.columnconfigure(0, weight=0, minsize=220)
    root.columnconfigure(1, weight=1)
    root.rowconfigure(0, weight=1)

    # -------------------------------------------------------------------------
    # SIDEBAR
    # -------------------------------------------------------------------------
    sidebar = tk.Frame(root, bg=current_theme["sidebar"], width=220)
    sidebar.grid(row=0, column=0, sticky="nsew")
    sidebar.grid_propagate(False)

    # App Title & Version
    brand_frame = tk.Frame(sidebar, bg=current_theme["sidebar"])
    brand_frame.pack(fill="x", padx=18, pady=(20, 15))

    title_lbl = tk.Label(
        brand_frame,
        text="⏪ REWIND",
        font=("Segoe UI", 15, "bold"),
        fg=current_theme["text"],
        bg=current_theme["sidebar"]
    )
    title_lbl.pack(anchor="w")

    ver_lbl = tk.Label(
        brand_frame,
        text=f"{APP_VERSION} Linux Edition",
        font=("Segoe UI", 9),
        fg=current_theme["text_muted"],
        bg=current_theme["sidebar"]
    )
    ver_lbl.pack(anchor="w")

    # Status badge
    status_frame = tk.Frame(sidebar, bg=current_theme["card"], bd=1, relief="solid")
    status_frame.pack(fill="x", padx=16, pady=(0, 20))

    status_lbl = tk.Label(
        status_frame,
        text="● Bufor aktywny",
        font=("Segoe UI", 9, "bold"),
        fg=current_theme["status_active"],
        bg=current_theme["card"],
        pady=5
    )
    status_lbl.pack(fill="x")

    def update_engine_status(active, text):
        def _ui():
            if active:
                status_lbl.config(text="● Bufor aktywny", fg=current_theme["status_active"])
            else:
                status_lbl.config(text="○ Bufor zatrzymany", fg=current_theme["text_muted"])
        root.after(0, _ui)

    engine.add_status_listener(update_engine_status)

    # Navigation buttons
    nav_buttons = []
    content_frames = {}

    def switch_tab(tab_id):
        for t_id, f in content_frames.items():
            if t_id == tab_id:
                f.pack(fill="both", expand=True, padx=24, pady=20)
            else:
                f.pack_forget()

        for btn, b_id in nav_buttons:
            if b_id == tab_id:
                btn.config(bg=current_theme["card"], fg=current_theme["text"], font=("Segoe UI", 10, "bold"))
            else:
                btn.config(bg=current_theme["sidebar"], fg=current_theme["text_muted"], font=("Segoe UI", 10))

    nav_items = [
        ("🎞️  Powtórki", "clips"),
        ("📹  Wideo i bufor", "video"),
        ("🔊  Dźwięk i mikser", "audio"),
        ("⌨️  Skróty klawiszowe", "hotkeys"),
        ("⚙️  Ustawienia", "settings"),
        ("ℹ️  O programie", "about")
    ]

    for label, tab_id in nav_items:
        btn = tk.Button(
            sidebar,
            text=label,
            font=("Segoe UI", 10),
            fg=current_theme["text_muted"],
            bg=current_theme["sidebar"],
            bd=0,
            anchor="w",
            padx=14,
            pady=8,
            cursor="hand2",
            activebackground=current_theme["card"],
            activeforeground=current_theme["text"],
            command=lambda tid=tab_id: switch_tab(tid)
        )
        btn.pack(fill="x", padx=10, pady=2)
        nav_buttons.append((btn, tab_id))

    # -------------------------------------------------------------------------
    # CONTENT CONTAINER
    # -------------------------------------------------------------------------
    content_container = tk.Frame(root, bg=current_theme["bg"])
    content_container.grid(row=0, column=1, sticky="nsew")

    # 1. VIEW: CLIPS
    view_clips = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["clips"] = view_clips

    # Hero Banner
    hero_card = tk.Frame(view_clips, bg=current_theme["card"], bd=1, relief="solid")
    hero_card.pack(fill="x", pady=(0, 16), ipady=12, ipadx=14)

    hero_hotkey_badge = tk.Label(
        hero_card,
        text=f"[{settings.Hotkey}]",
        font=("Segoe UI", 10, "bold"),
        fg=current_theme["accent"],
        bg=current_theme["pill_bg"],
        padx=8,
        pady=2
    )
    hero_hotkey_badge.pack(anchor="w", pady=(0, 4))

    hero_title = tk.Label(
        hero_card,
        text=f"Zapisz ostatnie {settings.ClipDuration} sekund",
        font=("Segoe UI", 16, "bold"),
        fg=current_theme["text"],
        bg=current_theme["card"]
    )
    hero_title.pack(anchor="w")

    hero_btn = tk.Button(
        hero_card,
        text="⚡ Zapisz powtórkę teraz",
        font=("Segoe UI", 11, "bold"),
        fg="#FFFFFF",
        bg=current_theme["accent"],
        bd=0,
        padx=16,
        pady=6,
        cursor="hand2",
        command=lambda: engine.save_clip()
    )
    hero_btn.pack(anchor="e", pady=(6, 0))

    # Clips List
    clips_header = tk.Frame(view_clips, bg=current_theme["bg"])
    clips_header.pack(fill="x", pady=(10, 6))

    tk.Label(
        clips_header,
        text="ZAPISANE KLIPY",
        font=("Segoe UI", 10, "bold"),
        fg=current_theme["text_muted"],
        bg=current_theme["bg"]
    ).pack(side="left")

    open_folder_btn = tk.Button(
        clips_header,
        text="📂 Otwórz folder",
        font=("Segoe UI", 9),
        fg=current_theme["text"],
        bg=current_theme["card"],
        bd=1,
        relief="solid",
        padx=8,
        pady=2,
        cursor="hand2",
        command=lambda: subprocess.Popen(["xdg-open", settings.ClipsFolder])
    )
    open_folder_btn.pack(side="right")

    clips_list_frame = tk.Frame(view_clips, bg=current_theme["bg"])
    clips_list_frame.pack(fill="both", expand=True)

    def refresh_clips_list():
        for widget in clips_list_frame.winfo_children():
            widget.destroy()

        clips_dir = Path(settings.ClipsFolder)
        if not clips_dir.exists():
            return

        mp4_files = sorted(clips_dir.glob("Clip_*.mp4"), key=lambda f: f.stat().st_mtime, reverse=True)
        if not mp4_files:
            tk.Label(
                clips_list_frame,
                text=f"Brak zapisanych powtórek. Naciśnij {settings.Hotkey}, aby utworzyć pierwszy klip!",
                font=("Segoe UI", 10),
                fg=current_theme["text_muted"],
                bg=current_theme["bg"],
                pady=30
            ).pack()
            return

        for f in mp4_files[:8]:
            row = tk.Frame(clips_list_frame, bg=current_theme["card"], bd=1, relief="solid")
            row.pack(fill="x", pady=4, ipady=6, ipadx=10)

            size_mb = f.stat().st_size / (1024 * 1024)
            name_lbl = tk.Label(
                row,
                text=f"🎬 {f.name} ({size_mb:.1f} MB)",
                font=("Segoe UI", 10, "bold"),
                fg=current_theme["text"],
                bg=current_theme["card"]
            )
            name_lbl.pack(side="left")

            del_btn = tk.Button(
                row,
                text="🗑️",
                font=("Segoe UI", 9),
                fg="#EF4444",
                bg=current_theme["pill_bg"],
                bd=0,
                padx=6,
                cursor="hand2",
                command=lambda p=f: delete_clip(p)
            )
            del_btn.pack(side="right", padx=4)

            trim_btn = tk.Button(
                row,
                text="✂️ Przytnij",
                font=("Segoe UI", 9),
                fg=current_theme["text"],
                bg=current_theme["pill_bg"],
                bd=0,
                padx=8,
                cursor="hand2",
                command=lambda p=f: show_trim_dialog(p)
            )
            trim_btn.pack(side="right", padx=4)

            play_btn = tk.Button(
                row,
                text="▶ Odtwórz",
                font=("Segoe UI", 9),
                fg=current_theme["text"],
                bg=current_theme["pill_bg"],
                bd=0,
                padx=8,
                cursor="hand2",
                command=lambda p=f: subprocess.Popen(["xdg-open", str(p)])
            )
            play_btn.pack(side="right", padx=4)

    def show_trim_dialog(file_path):
        dlg = tk.Toplevel(root)
        dlg.title("Szybkie przycinanie klipu")
        dlg.geometry("420x220")
        dlg.configure(bg=current_theme["card"])
        dlg.resizable(False, False)
        dlg.transient(root)
        dlg.grab_set()

        tk.Label(dlg, text="✂️ Szybkie przycinanie powtórki", font=("Segoe UI", 12, "bold"), fg=current_theme["text"], bg=current_theme["card"]).pack(anchor="w", padx=18, pady=(16, 2))
        tk.Label(dlg, text=file_path.name, font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w", padx=18, pady=(0, 14))

        fields_frame = tk.Frame(dlg, bg=current_theme["card"])
        fields_frame.pack(fill="x", padx=18, pady=(0, 16))

        # Start
        s_col = tk.Frame(fields_frame, bg=current_theme["card"])
        s_col.pack(side="left", fill="x", expand=True, padx=(0, 8))
        tk.Label(s_col, text="Początek (sekundy):", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")
        s_entry = tk.Entry(s_col, font=("Segoe UI", 11), bg=current_theme["pill_bg"], fg=current_theme["text"], bd=1, relief="solid")
        s_entry.insert(0, "0")
        s_entry.pack(fill="x", pady=4)

        # End
        e_col = tk.Frame(fields_frame, bg=current_theme["card"])
        e_col.pack(side="left", fill="x", expand=True, padx=(8, 0))
        tk.Label(e_col, text="Koniec (sekundy):", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")
        e_entry = tk.Entry(e_col, font=("Segoe UI", 11), bg=current_theme["pill_bg"], fg=current_theme["text"], bd=1, relief="solid")
        e_entry.insert(0, "15")
        e_entry.pack(fill="x", pady=4)

        btn_row = tk.Frame(dlg, bg=current_theme["card"])
        btn_row.pack(fill="x", padx=18, pady=(6, 16))

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

            success, res = engine.trim_clip(file_path, start_val, end_val)
            if success:
                dlg.destroy()
                refresh_clips_list()
                show_notification("Rewind", f"✂️ Klip przycięty: {Path(res).name}")
            else:
                messagebox.showerror("Błąd przycinania", res)

        save_btn = tk.Button(btn_row, text="⚡ Zapisz przycięty klip", font=("Segoe UI", 9, "bold"), fg="#FFFFFF", bg=current_theme["accent"], bd=0, padx=12, pady=6, cursor="hand2", command=on_save_trim)
        save_btn.pack(side="right")

        cancel_btn = tk.Button(btn_row, text="Anuluj", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["pill_bg"], bd=0, padx=12, pady=6, cursor="hand2", command=dlg.destroy)
        cancel_btn.pack(side="right", padx=8)

    def delete_clip(file_path):
        if messagebox.askyesno("Usuń klip", f"Czy na pewno chcesz usunąć {file_path.name}?"):
            try:
                file_path.unlink()
                refresh_clips_list()
            except Exception as e:
                messagebox.showerror("Błąd", str(e))

    def on_saving_start(dur):
        hero_btn.config(text="⏳ Zapisywanie...", state="disabled")

    def on_saving_done():
        hero_btn.config(text="⚡ Zapisz powtórkę teraz", state="normal")

    engine.add_clip_saving_listener(lambda dur: root.after(0, lambda: on_saving_start(dur)))
    engine.add_clip_saved_done_listener(lambda: root.after(0, on_saving_done))
    engine.add_clip_saved_listener(lambda path, dur: root.after(0, refresh_clips_list))
    refresh_clips_list()

    # 2. VIEW: VIDEO SETTINGS
    view_video = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["video"] = view_video

    def make_card(parent, title, subtitle):
        f = tk.Frame(parent, bg=current_theme["card"], bd=1, relief="solid")
        f.pack(fill="x", pady=6, ipady=8, ipadx=12)
        tk.Label(f, text=title, font=("Segoe UI", 11, "bold"), fg=current_theme["text"], bg=current_theme["card"]).pack(anchor="w")
        tk.Label(f, text=subtitle, font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w", pady=(0, 6))
        return f

    # Duration Card
    c_dur = make_card(view_video, "Długość bufora wideo", "Czas rejestrowany w pamięci podręcznej")
    dur_frame = tk.Frame(c_dur, bg=current_theme["card"])
    dur_frame.pack(anchor="w")

    for d in [15, 30, 60, 120]:
        b = tk.Button(
            dur_frame,
            text=f"{d}s",
            font=("Segoe UI", 9, "bold" if settings.ClipDuration == d else "normal"),
            bg=current_theme["pill_active"] if settings.ClipDuration == d else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.ClipDuration == d else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=d: set_duration(val)
        )
        b.pack(side="left", padx=3)

    def set_duration(d):
        settings.ClipDuration = d
        settings.save()
        hero_title.config(text=f"Zapisz ostatnie {d} sekund")
        switch_tab("video")

    # FPS Card
    c_fps = make_card(view_video, "Płynność nagrywania (FPS)", "Liczba klatek na sekundę dla bufora")
    fps_frame = tk.Frame(c_fps, bg=current_theme["card"])
    fps_frame.pack(anchor="w")

    for f in [30, 60, 120, 144]:
        b = tk.Button(
            fps_frame,
            text=f"{f} FPS",
            font=("Segoe UI", 9, "bold" if settings.Fps == f else "normal"),
            bg=current_theme["pill_active"] if settings.Fps == f else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.Fps == f else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=f: set_fps(val)
        )
        b.pack(side="left", padx=3)

    def set_fps(f):
        settings.Fps = f
        settings.save()
        engine.stop()
        engine.start()
        switch_tab("video")

    # Bitrate Card
    c_bit = make_card(view_video, "Jakość wideo (Bitrate)", "Przepływność strumienia wideo")
    bit_frame = tk.Frame(c_bit, bg=current_theme["card"])
    bit_frame.pack(anchor="w")

    for b_val in ["8M", "12M", "16M"]:
        b = tk.Button(
            bit_frame,
            text=f"{b_val.replace('M', ' Mbps')}",
            font=("Segoe UI", 9, "bold" if settings.Bitrate == b_val else "normal"),
            bg=current_theme["pill_active"] if settings.Bitrate == b_val else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.Bitrate == b_val else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=b_val: set_bitrate(val)
        )
        b.pack(side="left", padx=3)

    def set_bitrate(b_val):
        settings.Bitrate = b_val
        settings.save()
        engine.stop()
        engine.start()
        switch_tab("video")

    # GPU Acceleration Card
    c_gpu = make_card(view_video, "Akcelerator sprzętowy GPU", f"Wykryty silnik enkodera: {engine.encoder.upper()}")

    # 3. VIEW: AUDIO SETTINGS
    view_audio = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["audio"] = view_audio

    c_sysvol = make_card(view_audio, "Głośność dźwięku z komputera", "Dźwięki z gier, Discorda i systemu (PulseAudio / PipeWire)")
    sysvol_container = tk.Frame(c_sysvol, bg=current_theme["card"])
    sysvol_container.pack(anchor="w")

    def update_sys_vol(v):
        settings.SystemVolumePercent = v
        settings.save()

    c_micvol = make_card(view_audio, "Głośność mikrofonu", "Rejestrowanie Twojego głosu podczas rozgrywki")
    micvol_container = tk.Frame(c_micvol, bg=current_theme["card"])
    micvol_container.pack(anchor="w")

    def update_mic_vol(v):
        settings.MicVolumePercent = v
        settings.save()

    def update_audio_controls():
        for widget in sysvol_container.winfo_children():
            widget.destroy()
        for widget in micvol_container.winfo_children():
            widget.destroy()

        if settings.AdvancedMode:
            # Sys vol advanced entry (0-1000%)
            s_row = tk.Frame(sysvol_container, bg=current_theme["card"])
            s_row.pack(anchor="w")
            s_entry = tk.Entry(s_row, font=("Segoe UI", 10, "bold"), bg=current_theme["pill_bg"], fg=current_theme["text"], bd=1, relief="solid", width=8)
            s_entry.insert(0, str(settings.SystemVolumePercent))
            s_entry.pack(side="left")
            tk.Label(s_row, text="% (0 - 1000%)", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(side="left", padx=8)

            def apply_sys_vol(*args):
                try:
                    val = int(s_entry.get().replace("%", "").strip())
                    val = max(0, min(1000, val))
                    settings.SystemVolumePercent = val
                    settings.save()
                    s_entry.delete(0, "end")
                    s_entry.insert(0, str(val))
                except ValueError:
                    s_entry.delete(0, "end")
                    s_entry.insert(0, str(settings.SystemVolumePercent))

            def on_sys_key(event=None):
                try:
                    txt = s_entry.get().replace("%", "").strip()
                    if txt:
                        val = max(0, min(1000, int(txt)))
                        settings.SystemVolumePercent = val
                        settings.save()
                except:
                    pass

            s_btn = tk.Button(s_row, text="Zastosuj", font=("Segoe UI", 8, "bold"), bg=current_theme["accent"], fg="#FFFFFF", bd=0, padx=8, pady=2, cursor="hand2", command=apply_sys_vol)
            s_btn.pack(side="left", padx=4)
            s_entry.bind("<KeyRelease>", on_sys_key)
            s_entry.bind("<Return>", lambda e: apply_sys_vol())
            s_entry.bind("<FocusOut>", lambda e: apply_sys_vol())

            # Mic vol advanced entry (0-1000%)
            m_row = tk.Frame(micvol_container, bg=current_theme["card"])
            m_row.pack(anchor="w")
            m_entry = tk.Entry(m_row, font=("Segoe UI", 10, "bold"), bg=current_theme["pill_bg"], fg=current_theme["text"], bd=1, relief="solid", width=8)
            m_entry.insert(0, str(settings.MicVolumePercent))
            m_entry.pack(side="left")
            tk.Label(m_row, text="% (0 - 1000%)", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(side="left", padx=8)

            def apply_mic_vol(*args):
                try:
                    val = int(m_entry.get().replace("%", "").strip())
                    val = max(0, min(1000, val))
                    settings.MicVolumePercent = val
                    settings.save()
                    m_entry.delete(0, "end")
                    m_entry.insert(0, str(val))
                except ValueError:
                    m_entry.delete(0, "end")
                    m_entry.insert(0, str(settings.MicVolumePercent))

            def on_mic_key(event=None):
                try:
                    txt = m_entry.get().replace("%", "").strip()
                    if txt:
                        val = max(0, min(1000, int(txt)))
                        settings.MicVolumePercent = val
                        settings.save()
                except:
                    pass

            m_btn = tk.Button(m_row, text="Zastosuj", font=("Segoe UI", 8, "bold"), bg=current_theme["accent"], fg="#FFFFFF", bd=0, padx=8, pady=2, cursor="hand2", command=apply_mic_vol)
            m_btn.pack(side="left", padx=4)
            m_entry.bind("<KeyRelease>", on_mic_key)
            m_entry.bind("<Return>", lambda e: apply_mic_vol())
            m_entry.bind("<FocusOut>", lambda e: apply_mic_vol())
        else:
            is_updating = [True]
            try:
                sys_scale = tk.Scale(
                    sysvol_container,
                    from_=0,
                    to=150,
                    orient="horizontal",
                    bg=current_theme["card"],
                    fg=current_theme["text"],
                    highlightthickness=0,
                    bd=0,
                    length=350,
                    command=lambda v: update_sys_vol(int(v)) if not is_updating[0] else None
                )
                sys_scale.set(min(150, settings.SystemVolumePercent))
                sys_scale.pack(anchor="w")

                mic_scale = tk.Scale(
                    micvol_container,
                    from_=0,
                    to=150,
                    orient="horizontal",
                    bg=current_theme["card"],
                    fg=current_theme["text"],
                    highlightthickness=0,
                    bd=0,
                    length=350,
                    command=lambda v: update_mic_vol(int(v)) if not is_updating[0] else None
                )
                mic_scale.set(min(150, settings.MicVolumePercent))
                mic_scale.pack(anchor="w")
            finally:
                is_updating[0] = False

    update_audio_controls()

    c_snd = make_card(view_audio, "Dźwięk potwierdzenia", "Odtwarzaj dźwięk po pomyślnym utworzeniu klipu")
    test_btn = tk.Button(
        c_snd,
        text="🔔 Odtwórz dźwięk testowy",
        font=("Segoe UI", 9),
        bg=current_theme["pill_bg"],
        fg=current_theme["text"],
        bd=0,
        padx=10,
        pady=4,
        cursor="hand2",
        command=play_sound
    )
    test_btn.pack(anchor="w")

    c_multitrack = make_card(view_audio, "Osobne ścieżki audio (Multi-track)", "Zapisuj dźwięk z gry i mikrofon na oddzielnych ścieżkach w pliku MP4")
    track_var = tk.BooleanVar(value=settings.SeparateAudioTracks)

    def toggle_multitrack():
        settings.SeparateAudioTracks = track_var.get()
        settings.save()
        if engine.is_recording:
            engine.stop()
            engine.start()

    track_chk = tk.Checkbutton(
        c_multitrack,
        text="Włącz Multi-track (ścieżka 1: Gra/System, ścieżka 2: Mikrofon)",
        variable=track_var,
        font=("Segoe UI", 10),
        bg=current_theme["card"],
        fg=current_theme["text"],
        selectcolor=current_theme["pill_bg"],
        activebackground=current_theme["card"],
        activeforeground=current_theme["text"],
        command=toggle_multitrack
    )
    track_chk.pack(anchor="w")

    # 4. VIEW: HOTKEYS
    view_hotkeys = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["hotkeys"] = view_hotkeys

    c_hotkey = make_card(view_hotkeys, "Główny skrót klawiszowy", "Klawisz wyzwalający natychmiastowy zapis powtórki")
    hotkey_frame = tk.Frame(c_hotkey, bg=current_theme["card"])
    hotkey_frame.pack(anchor="w")

    for k in ["F7", "F8", "F9", "F10", "F11", "F12"]:
        b = tk.Button(
            hotkey_frame,
            text=k,
            font=("Segoe UI", 9, "bold" if settings.Hotkey == k else "normal"),
            bg=current_theme["pill_active"] if settings.Hotkey == k else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.Hotkey == k else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=k: set_hotkey(val)
        )
        b.pack(side="left", padx=3)

    def set_hotkey(k):
        settings.Hotkey = k
        settings.save()
        hero_hotkey_badge.config(text=f"[{k}]")
        switch_tab("hotkeys")

    c_sec_hotkey = make_card(view_hotkeys, "Drugi skrót (Długi klip)", "Dodatkowy skrót do zapisu dłuższej powtórki (np. całej rundy)")
    sec_var = tk.BooleanVar(value=settings.SecondaryHotkeyEnabled)

    def toggle_sec_hotkey():
        settings.SecondaryHotkeyEnabled = sec_var.get()
        settings.save()
        switch_tab("hotkeys")

    sec_chk = tk.Checkbutton(
        c_sec_hotkey,
        text="Włącz drugi skrót klawiszowy",
        variable=sec_var,
        font=("Segoe UI", 10),
        bg=current_theme["card"],
        fg=current_theme["text"],
        selectcolor=current_theme["pill_bg"],
        activebackground=current_theme["card"],
        activeforeground=current_theme["text"],
        command=toggle_sec_hotkey
    )
    sec_chk.pack(anchor="w", pady=(0, 6))

    sec_keys_frame = tk.Frame(c_sec_hotkey, bg=current_theme["card"])
    sec_keys_frame.pack(anchor="w", pady=(0, 6))

    for k in ["F7", "F8", "F9", "F10", "F11", "F12"]:
        b = tk.Button(
            sec_keys_frame,
            text=k,
            font=("Segoe UI", 9, "bold" if settings.SecondaryHotkey == k else "normal"),
            bg=current_theme["pill_active"] if settings.SecondaryHotkey == k else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.SecondaryHotkey == k else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=k: set_sec_hotkey(val)
        )
        b.pack(side="left", padx=3)

    def set_sec_hotkey(k):
        settings.SecondaryHotkey = k
        settings.save()
        switch_tab("hotkeys")

    tk.Label(c_sec_hotkey, text="Długość drugiej powtórki:", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w", pady=(4, 2))
    sec_dur_frame = tk.Frame(c_sec_hotkey, bg=current_theme["card"])
    sec_dur_frame.pack(anchor="w")

    for d in [60, 120, 180, 300]:
        b = tk.Button(
            sec_dur_frame,
            text=f"{d}s",
            font=("Segoe UI", 9, "bold" if settings.SecondaryClipDuration == d else "normal"),
            bg=current_theme["pill_active"] if settings.SecondaryClipDuration == d else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.SecondaryClipDuration == d else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=d: set_sec_dur(val)
        )
        b.pack(side="left", padx=3)

    def set_sec_dur(d):
        settings.SecondaryClipDuration = d
        settings.save()
        switch_tab("hotkeys")

    c_wayland = make_card(
        view_hotkeys,
        "Wskazówka dla środowisk Wayland (GNOME / KDE / Sway)",
        "Na Waylandzie możesz przypisać dowolne skróty w Ustawieniach Systemowych -> Skróty klawiszowe do poleceń:\n• Zwykły klip: rewind --save\n• Długi klip: rewind --save-long"
    )

    # 5. VIEW: SETTINGS
    view_settings = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["settings"] = view_settings

    c_adv = make_card(view_settings, "Tryb zaawansowany", "Odblokowuje precyzyjne pola wpisywania wartości parametrów")
    adv_var = tk.BooleanVar(value=settings.AdvancedMode)

    def toggle_adv():
        settings.AdvancedMode = adv_var.get()
        settings.save()
        update_audio_controls()

    adv_chk = tk.Checkbutton(
        c_adv,
        text="Włącz tryb zaawansowany",
        variable=adv_var,
        font=("Segoe UI", 10),
        bg=current_theme["card"],
        fg=current_theme["text"],
        selectcolor=current_theme["pill_bg"],
        activebackground=current_theme["card"],
        activeforeground=current_theme["text"],
        command=toggle_adv
    )
    adv_chk.pack(anchor="w")

    c_storage = make_card(view_settings, "Zarządzanie miejscem na dysku", "Automatyczne pilnowanie wolnej przestrzeni na dysku z powtórkami")
    stor_var = tk.BooleanVar(value=settings.StorageLimitEnabled)

    def toggle_storage():
        settings.StorageLimitEnabled = stor_var.get()
        settings.save()
        if settings.StorageLimitEnabled:
            engine.enforce_storage_limits()
            refresh_clips_list()
        switch_tab("settings")

    stor_chk = tk.Checkbutton(
        c_storage,
        text="Ogranicz rozmiar folderu powtórek",
        variable=stor_var,
        font=("Segoe UI", 10),
        bg=current_theme["card"],
        fg=current_theme["text"],
        selectcolor=current_theme["pill_bg"],
        activebackground=current_theme["card"],
        activeforeground=current_theme["text"],
        command=toggle_storage
    )
    stor_chk.pack(anchor="w", pady=(0, 6))

    tk.Label(c_storage, text="Maksymalny łączny rozmiar folderu z powtórkami:", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w", pady=(4, 2))
    gb_frame = tk.Frame(c_storage, bg=current_theme["card"])
    gb_frame.pack(anchor="w", pady=(0, 6))

    for g in [10, 25, 50, 100]:
        b = tk.Button(
            gb_frame,
            text=f"{g} GB",
            font=("Segoe UI", 9, "bold" if settings.MaxStorageLimitGB == g else "normal"),
            bg=current_theme["pill_active"] if settings.MaxStorageLimitGB == g else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.MaxStorageLimitGB == g else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=g: set_storage_gb(val)
        )
        b.pack(side="left", padx=3)

    def set_storage_gb(g):
        settings.MaxStorageLimitGB = g
        settings.save()
        if settings.StorageLimitEnabled:
            engine.enforce_storage_limits()
            refresh_clips_list()
        switch_tab("settings")

    tk.Label(c_storage, text="Automatyczne usuwanie starych klipów:", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w", pady=(4, 2))
    age_frame = tk.Frame(c_storage, bg=current_theme["card"])
    age_frame.pack(anchor="w")

    for a in [0, 7, 14, 30, 60]:
        label_a = "Wyłączone" if a == 0 else f"{a} dni"
        b = tk.Button(
            age_frame,
            text=label_a,
            font=("Segoe UI", 9, "bold" if settings.MaxClipAgeDays == a else "normal"),
            bg=current_theme["pill_active"] if settings.MaxClipAgeDays == a else current_theme["pill_bg"],
            fg="#FFFFFF" if settings.MaxClipAgeDays == a else current_theme["text"],
            bd=0,
            padx=10,
            pady=4,
            cursor="hand2",
            command=lambda val=a: set_storage_age(val)
        )
        b.pack(side="left", padx=3)

    def set_storage_age(a):
        settings.MaxClipAgeDays = a
        settings.save()
        if a > 0 or settings.StorageLimitEnabled:
            engine.enforce_storage_limits()
            refresh_clips_list()
        switch_tab("settings")

    c_ctrl = make_card(view_settings, "Zarządzanie stanem bufora", "Wstrzymaj lub wznów rejestrowanie w tle")
    pause_btn = tk.Button(
        c_ctrl,
        text="⏸ Wstrzymaj bufor" if engine.is_recording else "▶ Wznów bufor",
        font=("Segoe UI", 9, "bold"),
        bg=current_theme["pill_bg"],
        fg=current_theme["text"],
        bd=0,
        padx=12,
        pady=5,
        cursor="hand2",
        command=lambda: toggle_engine()
    )
    pause_btn.pack(anchor="w")

    def toggle_engine():
        if engine.is_recording:
            engine.stop()
            pause_btn.config(text="▶ Wznów bufor")
        else:
            engine.start()
            pause_btn.config(text="⏸ Wstrzymaj bufor")

    # 6. VIEW: ABOUT
    view_about = tk.Frame(content_container, bg=current_theme["bg"])
    content_frames["about"] = view_about

    c_abt = make_card(view_about, f"Rewind {APP_VERSION}", "Lekkie narzędzie powtórek wideo na Linuksa (X11 & Wayland)")
    tk.Label(c_abt, text="• Zużycie pamięci: ~45 MB RAM", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")
    tk.Label(c_abt, text="• Format nagrywania: MPEG-TS ring buffer (2s bloki)", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")
    tk.Label(c_abt, text=f"• Enkoder: {engine.encoder}", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")
    tk.Label(c_abt, text="• Autor: Rewind Project (Open Source)", font=("Segoe UI", 9), fg=current_theme["text_muted"], bg=current_theme["card"]).pack(anchor="w")

    # Default to Clips tab
    switch_tab("clips")

    # Start buffer on GUI launch if enabled
    if settings.BufferActive and not engine.is_recording:
        engine.start()

    # Window close handler
    def on_closing():
        # Keep engine running in daemon if minimized, or close
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
        if res:
            print("[Rewind] Polecenie zapisu wysłane do działającej instancji.")
            sys.exit(0)
        else:
            print("[Rewind] Rewind nie jest uruchomiony w tle! Uruchamianie...", file=sys.stderr)
            # Fallback: start engine, wait and save
            s = AppSettings.load()
            eng = BufferEngine(s)
            eng.start()
            time.sleep(2)
            eng.save_clip(s.ClipDuration)
            time.sleep(1)
            eng.stop()
            sys.exit(0)

    if args.save_long:
        res = send_ipc_command("SAVE_LONG")
        if res:
            print("[Rewind] Polecenie zapisu dłuższego klipu wysłane do działającej instancji.")
            sys.exit(0)
        else:
            print("[Rewind] Rewind nie jest uruchomiony w tle! Uruchamianie...", file=sys.stderr)
            s = AppSettings.load()
            eng = BufferEngine(s)
            eng.start()
            time.sleep(2)
            eng.save_clip(s.SecondaryClipDuration)
            time.sleep(1)
            eng.stop()
            sys.exit(0)

    if args.toggle:
        res = send_ipc_command("TOGGLE")
        print(f"[Rewind] Stan bufora przełączony: {res}")
        sys.exit(0)

    if args.status:
        res = send_ipc_command("STATUS")
        if res:
            print(f"[Rewind] {res}")
        else:
            print("[Rewind] Bufor jest nieaktywny (brak działającej instancji).")
        sys.exit(0)

    # Check if another GUI instance is already running
    if send_ipc_command("SHOW"):
        print("[Rewind] Istniejąca instancja programu została wywołana na pierwszy plan.")
        sys.exit(0)

    # Initialize assets and settings
    ensure_assets()
    settings = AppSettings.load()
    engine = BufferEngine(settings)

    # Start IPC server
    ipc = IpcServer(engine)
    ipc.start()

    # Global Hotkey listener for X11
    start_x11_hotkey_listener(engine)

    # Handle SIGUSR1 (allows `killall -USR1 rewind` to save clip)
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
        run_gui(settings, engine)
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
