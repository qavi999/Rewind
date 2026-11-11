using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ClipFlow {
    public enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    public enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    public class AudioDeviceInfo {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString() {
            return Name;
        }
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumeratorComObject { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator {
        int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceCollection {
        int GetCount(out uint pcDevices);
        int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice {
        int Activate([In, MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore {
        int GetCount(out uint cProps);
        int GetAt(uint iProp, out PROPERTYKEY pkey);
        int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PROPVARIANT {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pwszVal;
    }

    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioClient {
        int Initialize(int shareMode, int streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, [In, MarshalAs(UnmanagedType.LPStruct)] Guid audioSessionGuid);
        int GetBufferSize(out uint pNumBufferFrames);
        int GetStreamLatency(out long phnsLatency);
        int GetCurrentPadding(out uint pNumPaddingFrames);
        int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        int GetMixFormat(out IntPtr ppDeviceFormat);
        int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        int Start();
        int Stop();
        int Reset();
        int SetEventHandle(IntPtr eventHandle);
        int GetService([In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
    }

    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioRenderClient {
        int GetBuffer(uint numFramesRequested, out IntPtr ppData);
        int ReleaseBuffer(uint numFramesWritten, int dwFlags);
    }

    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioCaptureClient {
        int GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out int pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        int ReleaseBuffer(uint numFramesRead);
        int GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEXTENSIBLE {
        public WAVEFORMATEX Format;
        public ushort wValidBitsPerSample;
        public uint dwChannelMask;
        public Guid SubFormat;
    }

    public class WasapiLoopbackServer : IDisposable {
        [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint uPeriod);
        [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint uPeriod);
        [DllImport("propsys.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int PropVariantToString([In] ref PROPVARIANT propvar, [Out] StringBuilder psz, uint cch);

        private const int AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
        private const int AUDCLNT_BUFFERFLAGS_SILENT = 0x00000002;

        private readonly string _pipeName;
        private readonly string _fullPipePath;
        private Thread _workerThread;
        private volatile bool _isRunning;
        private NamedPipeServerStream _pipeServer;

        public volatile float SystemVolume = 1.0f;
        public volatile float MicVolume = 1.0f;
        public volatile string MicDeviceId = "default";
        public volatile bool MicEnabled = true;

        public int SampleRate { get; private set; }

        public string PipePath {
            get { return _fullPipePath; }
        }

        // Reusable zero-allocation extraction buffers for peak audio loop performance
        private float[] _sysTempFloats = new float[8192];
        private short[] _sysTempShorts = new short[8192];
        private float[] _micTempFloats = new float[8192];
        private short[] _micTempShorts = new short[8192];
        private byte[] _micTempBytes = new byte[16384];

        public WasapiLoopbackServer(float sysVol, float micVol, string micId) {
            _pipeName = "clipflow_audio_" + Guid.NewGuid().ToString("N");
            _fullPipePath = @"\\.\pipe\" + _pipeName;
            SystemVolume = sysVol;
            MicVolume = micVol;
            MicDeviceId = string.IsNullOrEmpty(micId) ? "default" : micId;
            MicEnabled = (MicDeviceId != "none");
            SampleRate = 48000;
        }

        public static List<AudioDeviceInfo> GetMicrophoneDevices() {
            var list = new List<AudioDeviceInfo>();
            list.Add(new AudioDeviceInfo { Id = "default", Name = "Domyślny mikrofon systemowy" });

            try {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDeviceCollection coll;
                int hr = enumerator.EnumAudioEndpoints(EDataFlow.eCapture, 1 /* DEVICE_STATE_ACTIVE */, out coll);
                if (hr == 0 && coll != null) {
                    uint count;
                    coll.GetCount(out count);
                    for (uint i = 0; i < count; i++) {
                        IMMDevice dev;
                        if (coll.Item(i, out dev) == 0 && dev != null) {
                            string id;
                            dev.GetId(out id);
                            string name = GetDeviceFriendlyName(dev);
                            if (string.IsNullOrEmpty(name)) name = "Mikrofon #" + (i + 1);
                            list.Add(new AudioDeviceInfo { Id = id, Name = name });
                        }
                    }
                }
            } catch { }

            list.Add(new AudioDeviceInfo { Id = "none", Name = "Brak / Wyłączony mikrofon" });
            return list;
        }

        private static string GetDeviceFriendlyName(IMMDevice dev) {
            try {
                IPropertyStore props;
                if (dev.OpenPropertyStore(0, out props) == 0 && props != null) {
                    uint count;
                    props.GetCount(out count);
                    for (uint i = 0; i < count; i++) {
                        PROPERTYKEY key;
                        props.GetAt(i, out key);
                        if (key.pid == 14) { // PKEY_Device_FriendlyName
                            PROPVARIANT pv;
                            props.GetValue(ref key, out pv);
                            var sb = new StringBuilder(256);
                            PropVariantToString(ref pv, sb, 256);
                            if (sb.Length > 0) return sb.ToString();
                        }
                    }
                }
            } catch { }
            return null;
        }

        public void Start() {
            if (_isRunning) return;
            _isRunning = true;
            _workerThread = new Thread(LoopbackWorker) {
                IsBackground = true,
                Name = "ClipFlow_AudioMixer"
            };
            _workerThread.Start();
        }

        public void Stop() {
            _isRunning = false;
            try {
                if (_pipeServer != null) {
                    _pipeServer.Close();
                }
            } catch { }

            if (_workerThread != null && _workerThread.IsAlive) {
                try {
                    _workerThread.Join(300);
                } catch { }
            }
        }

        private void LoopbackWorker() {
            timeBeginPeriod(1);

            IAudioClient renderClient = null;
            IAudioRenderClient audioRender = null;
            uint renderBufferSize = 0;

            IAudioClient loopClient = null;
            IAudioCaptureClient loopCapture = null;
            IntPtr pSysFormat = IntPtr.Zero;

            IAudioClient micClient = null;
            IAudioCaptureClient micCapture = null;
            IntPtr pMicFormat = IntPtr.Zero;

            int sysChannels = 2;
            int sysBitsPerSample = 32;
            bool sysIsFloat = true;

            int micChannels = 2;
            int micBitsPerSample = 32;
            bool micIsFloat = true;

            try {
                _pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    131072,
                    131072
                );

                var asyncResult = _pipeServer.BeginWaitForConnection(null, null);
                while (_isRunning && !asyncResult.IsCompleted) {
                    Thread.Sleep(15);
                }

                if (!_isRunning) return;
                _pipeServer.EndWaitForConnection(asyncResult);

                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                Guid iidClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
                Guid iidRender = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                Guid iidCapture = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
                Guid emptyGuid = Guid.Empty;

                // 1. Initialize Render and Silent Client (To keep WASAPI loopback running continuously!)
                IMMDevice dev;
                if (enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out dev) == 0 && dev != null) {
                    IntPtr pRenderClientObj;
                    if (dev.Activate(iidClient, 1, IntPtr.Zero, out pRenderClientObj) == 0 && pRenderClientObj != IntPtr.Zero) {
                        renderClient = (IAudioClient)Marshal.GetObjectForIUnknown(pRenderClientObj);
                        renderClient.GetMixFormat(out pSysFormat);

                        var wf = (WAVEFORMATEX)Marshal.PtrToStructure(pSysFormat, typeof(WAVEFORMATEX));
                        SampleRate = (int)wf.nSamplesPerSec;
                        sysChannels = wf.nChannels;
                        sysBitsPerSample = wf.wBitsPerSample;
                        sysIsFloat = (wf.wFormatTag == 3 /* IEEE_FLOAT */ || wf.wFormatTag == 0xFFFE);

                        if (renderClient.Initialize(0, 0, 10000000, 0, pSysFormat, emptyGuid) == 0) {
                            IntPtr pRender;
                            if (renderClient.GetService(iidRender, out pRender) == 0 && pRender != IntPtr.Zero) {
                                audioRender = (IAudioRenderClient)Marshal.GetObjectForIUnknown(pRender);
                                renderClient.GetBufferSize(out renderBufferSize);
                                IntPtr pRData;
                                if (audioRender.GetBuffer(renderBufferSize, out pRData) == 0) {
                                    audioRender.ReleaseBuffer(renderBufferSize, AUDCLNT_BUFFERFLAGS_SILENT);
                                }
                                renderClient.Start();
                            }
                        }
                    }

                    // 2. Initialize Loopback Capture on the same render device
                    IntPtr pLoopClientObj;
                    if (dev.Activate(iidClient, 1, IntPtr.Zero, out pLoopClientObj) == 0 && pLoopClientObj != IntPtr.Zero) {
                        loopClient = (IAudioClient)Marshal.GetObjectForIUnknown(pLoopClientObj);
                        if (loopClient.Initialize(0, AUDCLNT_STREAMFLAGS_LOOPBACK, 10000000, 0, pSysFormat, emptyGuid) == 0) {
                            IntPtr pCap;
                            if (loopClient.GetService(iidCapture, out pCap) == 0 && pCap != IntPtr.Zero) {
                                loopCapture = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(pCap);
                                loopClient.Start();
                            }
                        }
                    }
                }

                // 3. Initialize Microphone Audio (if enabled)
                if (MicEnabled && MicDeviceId != "none") {
                    try {
                        IMMDevice micDev = null;
                        if (MicDeviceId == "default") {
                            enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eMultimedia, out micDev);
                        } else {
                            enumerator.GetDevice(MicDeviceId, out micDev);
                        }

                        if (micDev != null) {
                            IntPtr pMicClientObj;
                            if (micDev.Activate(iidClient, 1, IntPtr.Zero, out pMicClientObj) == 0 && pMicClientObj != IntPtr.Zero) {
                                micClient = (IAudioClient)Marshal.GetObjectForIUnknown(pMicClientObj);
                                micClient.GetMixFormat(out pMicFormat);

                                var mwf = (WAVEFORMATEX)Marshal.PtrToStructure(pMicFormat, typeof(WAVEFORMATEX));
                                micChannels = mwf.nChannels;
                                micBitsPerSample = mwf.wBitsPerSample;
                                micIsFloat = (mwf.wFormatTag == 3 || mwf.wFormatTag == 0xFFFE);

                                if (micClient.Initialize(0, 0, 10000000, 0, pMicFormat, emptyGuid) == 0) {
                                    IntPtr pCap;
                                    if (micClient.GetService(iidCapture, out pCap) == 0 && pCap != IntPtr.Zero) {
                                        micCapture = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(pCap);
                                        micClient.Start();
                                    }
                                }
                            }
                        }
                    } catch { }
                }

                // Mixing & Streaming Buffers
                var micFifo = new Queue<float>(19200);
                float[] mixFloats = new float[3840]; // 40ms stereo buffer
                byte[] byteBuffer = new byte[mixFloats.Length * 4];

                while (_isRunning && _pipeServer.IsConnected) {
                    // A. Keep Silent Render Stream Alive (Crucial: prevents WASAPI loopback starvation!)
                    if (renderClient != null && audioRender != null) {
                        uint padding;
                        if (renderClient.GetCurrentPadding(out padding) == 0) {
                            uint toFill = renderBufferSize - padding;
                            if (toFill > 0) {
                                IntPtr pRData;
                                if (audioRender.GetBuffer(toFill, out pRData) == 0) {
                                    audioRender.ReleaseBuffer(toFill, AUDCLNT_BUFFERFLAGS_SILENT);
                                }
                            }
                        }
                    }

                    // B. Drain all available packets from Microphone
                    if (micCapture != null && MicEnabled && MicVolume > 0.001f) {
                        uint micPkt;
                        while (micCapture.GetNextPacketSize(out micPkt) == 0 && micPkt > 0) {
                            IntPtr pData; uint frames; int flags; ulong pos, qpc;
                            if (micCapture.GetBuffer(out pData, out frames, out flags, out pos, out qpc) == 0) {
                                if (frames > 0) {
                                    if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0) {
                                        for (int i = 0; i < frames * 2; i++) {
                                            micFifo.Enqueue(0f);
                                        }
                                    } else {
                                        ExtractMicSamplesToFifo(pData, frames, micChannels, micBitsPerSample, micIsFloat, MicVolume, micFifo);
                                    }
                                    micCapture.ReleaseBuffer(frames);
                                }
                            }
                        }
                        // Bound mic FIFO to avoid drift/latency (max ~150ms buffer)
                        while (micFifo.Count > 14400) {
                            micFifo.Dequeue();
                        }
                    }

                    // C. Drain all available packets from Loopback Capture & Mix
                    bool hadPackets = false;
                    if (loopCapture != null) {
                        uint loopPkt;
                        while (loopCapture.GetNextPacketSize(out loopPkt) == 0 && loopPkt > 0) {
                            hadPackets = true;
                            IntPtr pData; uint frames; int flags; ulong pos, qpc;
                            if (loopCapture.GetBuffer(out pData, out frames, out flags, out pos, out qpc) == 0) {
                                if (frames > 0) {
                                    int samples = (int)frames * 2; // Stereo output
                                    if (mixFloats.Length < samples) {
                                        mixFloats = new float[samples * 2];
                                        byteBuffer = new byte[mixFloats.Length * 4];
                                    }

                                    float curSysVol = SystemVolume;
                                    bool isSysSilent = ((flags & AUDCLNT_BUFFERFLAGS_SILENT) != 0) || (curSysVol < 0.001f);

                                    if (isSysSilent) {
                                        Array.Clear(mixFloats, 0, samples);
                                    } else {
                                        ExtractSystemSamples(pData, frames, sysChannels, sysBitsPerSample, sysIsFloat, curSysVol, mixFloats);
                                    }

                                    // Mix in microphone
                                    for (int i = 0; i < samples; i += 2) {
                                        float micL = 0f;
                                        float micR = 0f;
                                        if (micFifo.Count >= 2) {
                                            micL = micFifo.Dequeue();
                                            micR = micFifo.Dequeue();
                                        } else if (micFifo.Count == 1) {
                                            micL = micFifo.Dequeue();
                                            micR = micL;
                                        }

                                        float outL = mixFloats[i] + micL;
                                        float outR = mixFloats[i + 1] + micR;

                                        // Soft limiter (prevent harsh digital distortion)
                                        if (outL > 1.0f) outL = 1.0f; else if (outL < -1.0f) outL = -1.0f;
                                        if (outR > 1.0f) outR = 1.0f; else if (outR < -1.0f) outR = -1.0f;

                                        mixFloats[i] = outL;
                                        mixFloats[i + 1] = outR;
                                    }

                                    int bytesToWrite = samples * 4;
                                    Buffer.BlockCopy(mixFloats, 0, byteBuffer, 0, bytesToWrite);
                                    _pipeServer.Write(byteBuffer, 0, bytesToWrite);

                                    loopCapture.ReleaseBuffer(frames);
                                }
                            }
                        }
                    }

                    if (!hadPackets) {
                        Thread.Sleep(3);
                    }
                }
            } catch {
            } finally {
                timeEndPeriod(1);

                try { if (renderClient != null) renderClient.Stop(); } catch { }
                try { if (loopClient != null) loopClient.Stop(); } catch { }
                try { if (micClient != null) micClient.Stop(); } catch { }

                if (pSysFormat != IntPtr.Zero) {
                    try { Marshal.FreeCoTaskMem(pSysFormat); } catch { }
                }
                if (pMicFormat != IntPtr.Zero) {
                    try { Marshal.FreeCoTaskMem(pMicFormat); } catch { }
                }

                try {
                    if (_pipeServer != null) {
                        _pipeServer.Dispose();
                        _pipeServer = null;
                    }
                } catch { }
            }
        }

        private void ExtractSystemSamples(
            IntPtr pData,
            uint frames,
            int channels,
            int bitsPerSample,
            bool isFloat,
            float volume,
            float[] output)
        {
            int totalInputFloats = (int)frames * channels;

            if (isFloat && bitsPerSample == 32) {
                if (_sysTempFloats.Length < totalInputFloats) {
                    _sysTempFloats = new float[totalInputFloats * 2];
                }
                Marshal.Copy(pData, _sysTempFloats, 0, totalInputFloats);

                if (channels == 2) {
                    for (int i = 0; i < frames * 2; i++) {
                        output[i] = _sysTempFloats[i] * volume;
                    }
                } else if (channels == 1) {
                    for (int i = 0; i < frames; i++) {
                        float v = _sysTempFloats[i] * volume;
                        output[i * 2] = v;
                        output[i * 2 + 1] = v;
                    }
                } else {
                    // Downmix 5.1 / 7.1 to stereo
                    for (int i = 0; i < frames; i++) {
                        int baseIdx = i * channels;
                        float fl = _sysTempFloats[baseIdx];
                        float fr = _sysTempFloats[baseIdx + 1];
                        float fc = channels > 2 ? _sysTempFloats[baseIdx + 2] * 0.707f : 0f;
                        float sl = channels > 4 ? _sysTempFloats[baseIdx + 4] * 0.707f : 0f;
                        float sr = channels > 5 ? _sysTempFloats[baseIdx + 5] * 0.707f : 0f;

                        output[i * 2] = (fl + fc + sl) * volume;
                        output[i * 2 + 1] = (fr + fc + sr) * volume;
                    }
                }
            } else if (bitsPerSample == 16) {
                if (_sysTempShorts.Length < totalInputFloats) {
                    _sysTempShorts = new short[totalInputFloats * 2];
                }
                Marshal.Copy(pData, _sysTempShorts, 0, totalInputFloats);
                const float inv16 = 1.0f / 32768.0f;

                if (channels == 2) {
                    for (int i = 0; i < frames * 2; i++) {
                        output[i] = (_sysTempShorts[i] * inv16) * volume;
                    }
                } else {
                    for (int i = 0; i < frames; i++) {
                        float v = (_sysTempShorts[i * channels] * inv16) * volume;
                        output[i * 2] = v;
                        output[i * 2 + 1] = v;
                    }
                }
            } else {
                Array.Clear(output, 0, (int)frames * 2);
            }
        }

        private void ExtractMicSamplesToFifo(
            IntPtr pData,
            uint frames,
            int channels,
            int bitsPerSample,
            bool isFloat,
            float volume,
            Queue<float> fifo)
        {
            if (frames == 0) return;
            int totalRawSamples = (int)frames * channels;

            if (isFloat && bitsPerSample == 32) {
                if (_micTempFloats.Length < totalRawSamples) {
                    _micTempFloats = new float[totalRawSamples * 2];
                }
                Marshal.Copy(pData, _micTempFloats, 0, totalRawSamples);
                for (int i = 0; i < frames; i++) {
                    float sL = _micTempFloats[i * channels] * volume;
                    float sR = channels > 1 ? _micTempFloats[i * channels + 1] * volume : sL;
                    fifo.Enqueue(sL);
                    fifo.Enqueue(sR);
                }
            } else if (bitsPerSample == 16) {
                if (_micTempShorts.Length < totalRawSamples) {
                    _micTempShorts = new short[totalRawSamples * 2];
                }
                Marshal.Copy(pData, _micTempShorts, 0, totalRawSamples);
                const float inv16 = 1.0f / 32768.0f;
                for (int i = 0; i < frames; i++) {
                    float sL = (_micTempShorts[i * channels] * inv16) * volume;
                    float sR = channels > 1 ? (_micTempShorts[i * channels + 1] * inv16) * volume : sL;
                    fifo.Enqueue(sL);
                    fifo.Enqueue(sR);
                }
            } else if (bitsPerSample == 24) {
                int byteCount = totalRawSamples * 3;
                if (_micTempBytes.Length < byteCount) {
                    _micTempBytes = new byte[byteCount * 2];
                }
                Marshal.Copy(pData, _micTempBytes, 0, byteCount);
                const float inv24 = 1.0f / 8388608.0f;
                int idx = 0;
                for (int i = 0; i < frames; i++) {
                    int b0 = _micTempBytes[idx++]; int b1 = _micTempBytes[idx++]; int b2 = (sbyte)_micTempBytes[idx++];
                    int rawL = (b2 << 16) | (b1 << 8) | b0;
                    float sL = (rawL * inv24) * volume;
                    float sR = sL;
                    if (channels > 1) {
                        int rb0 = _micTempBytes[idx++]; int rb1 = _micTempBytes[idx++]; int rb2 = (sbyte)_micTempBytes[idx++];
                        int rawR = (rb2 << 16) | (rb1 << 8) | rb0;
                        sR = (rawR * inv24) * volume;
                    }
                    fifo.Enqueue(sL);
                    fifo.Enqueue(sR);
                }
            } else {
                for (int i = 0; i < frames * 2; i++) {
                    fifo.Enqueue(0f);
                }
            }
        }

        public void Dispose() {
            Stop();
        }
    }
}
