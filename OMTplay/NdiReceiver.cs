using System.Runtime.InteropServices;
using libomtnet;

namespace OMTplay;

public sealed record NdiSource(string Name, string Url);

// ABI declarations follow the NDI 6.3.2 SDK headers; x64, C calling convention.
internal static class NdiNative
{
    private const string Dll = "Processing.NDI.Lib.x64.dll";
    private static readonly Lazy<bool> Initialized = new(() => NDIlib_initialize());
    internal static void Initialize()
    {
        if (!Initialized.Value) throw new InvalidOperationException("NDI runtime initialization failed.");
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Source { public IntPtr Name, Url; }
    [StructLayout(LayoutKind.Sequential)] internal struct FindSettings
    {
        [MarshalAs(UnmanagedType.I1)] public bool ShowLocal;
        public IntPtr Groups, ExtraIps;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct ReceiveSettings
    {
        public Source Source;
        public int ColorFormat, Bandwidth;
        [MarshalAs(UnmanagedType.I1)] public bool AllowFields;
        public IntPtr Name;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Video
    {
        public int Width, Height, FourCC, RateN, RateD;
        public float Aspect;
        public int FrameFormat;
        public long Timecode;
        public IntPtr Data;
        public int Stride;
        public IntPtr Metadata;
        public long Timestamp;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Audio
    {
        public int SampleRate, Channels, Samples;
        public long Timecode;
        public IntPtr Data;
        public int ChannelStride;
        public IntPtr Metadata;
        public long Timestamp;
    }
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)] private static extern bool NDIlib_initialize();
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr NDIlib_find_create_v2(ref FindSettings settings);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr NDIlib_find_get_current_sources(IntPtr finder, out uint count);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void NDIlib_find_destroy(IntPtr finder);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr NDIlib_recv_create_v3(ref ReceiveSettings settings);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void NDIlib_recv_destroy(IntPtr receiver);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int NDIlib_recv_capture_v2(IntPtr receiver, ref Video video, ref Audio audio, IntPtr metadata, uint timeout);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void NDIlib_recv_free_video_v2(IntPtr receiver, ref Video video);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void NDIlib_recv_free_audio_v2(IntPtr receiver, ref Audio audio);
}

public sealed class NdiDiscovery : IDisposable
{
    private IntPtr handle;
    public NdiDiscovery()
    {
        NdiNative.Initialize();
        var settings = new NdiNative.FindSettings { ShowLocal = true };
        handle = NdiNative.NDIlib_find_create_v2(ref settings);
        if (handle == IntPtr.Zero) throw new InvalidOperationException("Cannot create NDI discovery.");
    }
    // Caller serializes discovery calls; copy native strings before the next query.
    public NdiSource[] GetSources()
    {
        ObjectDisposedException.ThrowIf(handle == IntPtr.Zero, this);
        var pointer = NdiNative.NDIlib_find_get_current_sources(handle, out uint count);
        var sources = new List<NdiSource>();
        for (int i = 0; i < count; i++)
        {
            var source = Marshal.PtrToStructure<NdiNative.Source>(IntPtr.Add(pointer, i * Marshal.SizeOf<NdiNative.Source>()));
            string? name = Marshal.PtrToStringUTF8(source.Name);
            if (!string.IsNullOrEmpty(name)) sources.Add(new(name, Marshal.PtrToStringUTF8(source.Url) ?? ""));
        }
        return sources.ToArray();
    }
    public void Dispose()
    {
        if (handle != IntPtr.Zero) { NdiNative.NDIlib_find_destroy(handle); handle = IntPtr.Zero; }
    }
}

public sealed class NdiReceiver : IDisposable
{
    private IntPtr handle;
    public NdiReceiver(NdiSource source, bool lowBandwidth = false)
    {
        NdiNative.Initialize();
        var name = Marshal.StringToCoTaskMemUTF8(source.Name);
        var url = Marshal.StringToCoTaskMemUTF8(source.Url);
        var receiverName = Marshal.StringToCoTaskMemUTF8("OMTplay");
        try
        {
            var settings = new NdiNative.ReceiveSettings
            {
                Source = new() { Name = name, Url = url }, ColorFormat = 0,
                Bandwidth = lowBandwidth ? 0 : 100, AllowFields = false, Name = receiverName
            };
            handle = NdiNative.NDIlib_recv_create_v3(ref settings);
            if (handle == IntPtr.Zero) throw new InvalidOperationException("Cannot create NDI receiver.");
        }
        finally { Marshal.FreeCoTaskMem(name); Marshal.FreeCoTaskMem(url); Marshal.FreeCoTaskMem(receiverName); }
    }

    // Callbacks consume/copy the data synchronously, before returning the frame to NDI.
    public void Capture(Action<OMTMediaFrame> video, Action<float[], int, int, int> audio)
    {
        ObjectDisposedException.ThrowIf(handle == IntPtr.Zero, this);
        var v = new NdiNative.Video(); var a = new NdiNative.Audio();
        int type = NdiNative.NDIlib_recv_capture_v2(handle, ref v, ref a, IntPtr.Zero, 20);
        if (type == 1)
        {
            try
            {
                if (v.FourCC != 0x41524742 && v.FourCC != 0x58524742) return; // BGRA/BGRX
                int stride = v.Stride == 0 ? checked(v.Width * 4) : v.Stride;
                video(new OMTMediaFrame { Width = v.Width, Height = v.Height, Stride = stride,
                    Data = v.Data, DataLength = checked(Math.Abs(stride) * v.Height),
                    FrameRateN = v.RateN, FrameRateD = v.RateD, Timestamp = Math.Max(0, v.Timecode) });
            }
            finally { NdiNative.NDIlib_recv_free_video_v2(handle, ref v); }
        }
        else if (type == 2)
        {
            try
            {
                if (a.Data == IntPtr.Zero || a.Channels < 1 || a.Channels > 32 || a.Samples <= 0 ||
                    a.SampleRate < 8000 || a.SampleRate > 192000 || a.Samples > a.SampleRate * 2) return;
                int stride = a.ChannelStride == 0 ? checked(a.Samples * 4) : a.ChannelStride;
                if (stride < a.Samples * 4) return;
                float[] planar = new float[checked(a.Channels * a.Samples)];
                for (int c = 0; c < a.Channels; c++)
                    Marshal.Copy(IntPtr.Add(a.Data, checked(c * stride)), planar, c * a.Samples, a.Samples);
                audio(planar, a.SampleRate, a.Channels, a.Samples);
            }
            finally { NdiNative.NDIlib_recv_free_audio_v2(handle, ref a); }
        }
        else if (type == 4) throw new IOException("NDI connection lost.");
    }
    // Only called after the capture thread has stopped.
    public void Dispose()
    {
        if (handle != IntPtr.Zero) { NdiNative.NDIlib_recv_destroy(handle); handle = IntPtr.Zero; }
    }
}
