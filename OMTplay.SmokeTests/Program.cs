using System.Reflection;
using System.Runtime.InteropServices;
using libomtnet;
using NAudio.Wave;
using OMTplay;

internal static class Program
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(Form1 f, string name, params object[] args) => typeof(Form1).GetMethod(name, Private)!.Invoke(f, args);
    static T Field<T>(Form1 f, string name) => (T)typeof(Form1).GetField(name, Private)!.GetValue(f)!;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            using var form = new Form1();
            try
            {
                // Regression: a second stereo packet must reuse the same output device.
                Call(form, "EnsureAudioInit", 48000, 2);
                var output = Field<IWavePlayer?>(form, "_waveOut");
                Check(output != null, "Audio output initialized");
                Check(Field<bool>(form, "_audioInitDone"), "Stereo initialization completes");
                for (int i = 0; i < 100; i++) Call(form, "EnsureAudioInit", 48000, 2);
                Check(ReferenceEquals(output, Field<IWavePlayer>(form, "_waveOut")), "100 stereo packets reuse output");
                Call(form, "EnsureAudioInit", 44100, 8);
                Check(!ReferenceEquals(output, Field<IWavePlayer>(form, "_waveOut")), "Format change recreates output");
                Check(Field<BufferedWaveProvider>(form, "_audioBuffer").WaveFormat.SampleRate == 44100, "New sample rate applied");

                // Test exact sample routing with a buffer disconnected from the audio device.
                var buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                typeof(Form1).GetField("_audioBuffer", Private)!.SetValue(form, buffer);
                typeof(Form1).GetField("_deviceAcceptsFloat", Private)!.SetValue(form, true);
                typeof(Form1).GetField("_audioSelection", Private)!.SetValue(form, 3);
                var data = Marshal.AllocHGlobal(8 * 2 * 4);
                try
                {
                    float[] samples = Enumerable.Range(0, 16).Select(i => i / 100f).ToArray();
                    Marshal.Copy(samples, 0, data, samples.Length);
                    Call(form, "ProcessAudio", new OMTMediaFrame { Data = data, DataLength = 64, Channels = 8, SamplesPerChannel = 2, SampleRate = 44100 });
                    byte[] bytes = new byte[16]; buffer.Read(bytes, 0, 16);
                    var actual = new float[4]; Buffer.BlockCopy(bytes, 0, actual, 0, 16);
                    Check(actual.SequenceEqual(new[] { .12f, .14f, .13f, .15f }), "CH7/8 planar audio routes correctly");
                }
                finally { Marshal.FreeHGlobal(data); }
                TestBitmap();
                TestRouting();
                TestEightChannelMeter(form);
                TestNavigation(form);
                TestApi(form);
            }
            finally { Call(form, "Form1_FormClosing", form, new FormClosingEventArgs(CloseReason.UserClosing, false)); }
            if (!args.Contains("--offline"))
            {
                TestNetwork();
                TestNdiAndMixedSwitching();
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static void TestEightChannelMeter(Form1 form)
    {
        Call(form, "Disconnect");
        // Metering must work even without an output device, and before the stereo/mono routing.
        typeof(Form1).GetField("_audioPermanentlyDisabled", Private)!.SetValue(form, true);
        float[] planar = Enumerable.Range(1, 8).SelectMany(c => new[] { c / 10f, -c / 10f }).ToArray();
        Call(form, "ProcessPlanarAudio", planar, 48000, 8, 2);
        Check(Field<float[]>(form, "_inputPeaks").SequenceEqual(Enumerable.Range(1, 8).Select(c => c / 10f)),
            "Eight independent input peaks work without playback device");
        Call(form, "DisplayLatestFrame");
        var meter = Field<AudioLevelMeter>(form, "_audioMeter");
        var levels = (float[])typeof(AudioLevelMeter).GetField("levels", Private)!.GetValue(meter)!;
        Check(levels.Length == 8 && Math.Abs(levels[7] - AudioLevelMeter.ToDecibels(.8f)) < .001f,
            "Channel 8 reaches the displayed input meter");
        var selection = Field<ComboBox>(form, "_cbAudio");
        selection.SelectedIndex = 4;
        Check(levels[7] > -60, "Changing listening selection does not reset input meters");
        Call(form, "ProcessPlanarAudio", new float[] { .1f, .2f }, 48000, 2, 1);
        Call(form, "DisplayLatestFrame");
        Check(levels.Skip(2).All(db => db == -60), "Stereo source clears absent channels 3 to 8");
        meter.UpdatePeaks(new float[] { .01f, .04f, .1f, .2f, .4f, .6f, .8f, 1f }, 8);
        using (var preview = new Bitmap(meter.Width, meter.Height))
        {
            meter.DrawToBitmap(preview, new Rectangle(Point.Empty, meter.Size));
            preview.Save(Path.Combine(AppContext.BaseDirectory, "meter8-preview.png"));
        }
        Call(form, "Disconnect");
        Check(levels.All(db => db == -60) && Field<int>(form, "_meterChannels") == 0,
            "Disconnect resets all eight meters");
    }

    static T Pump<T>(Task<T> task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("API test timed out.");
            Application.DoEvents();
            Thread.Sleep(5);
        }
        return task.GetAwaiter().GetResult();
    }

    static void TestApi(Form1 form)
    {
        _ = form.Handle;
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        System.Net.Http.HttpResponseMessage Request(string function) => Pump(client.GetAsync($"http://127.0.0.1:{port}/api/?Function={function}"));
        Check(Pump(form.ConfigureApiAsync(true, port)), "API starts on configured port");
        try
        {
            var sources = Field<ComboBox>(form, "_cbSources");
            sources.Items.Clear();
            using (var response = Request("invalid")) Check((int)response.StatusCode == 400, "Unknown API function returns 400");
            using (var response = Request("connect")) Check((int)response.StatusCode == 409, "API connect without selection returns 409");
            using var first = new OMTSend("API Test A", OMTQuality.Default);
            using var second = new OMTSend("API Test B", OMTQuality.Default);
            sources.Items.AddRange(new object[] { first.URL, second.URL });
            sources.SelectedIndex = 0;
            foreach (var function in new[] { "connect", "fullOn", "fullOn", "next", "prev", "fullOFF", "fullOFF" })
            {
                using var response = Request(function);
                Check(response.IsSuccessStatusCode, $"HTTP API {function} succeeds");
                if (function == "next") Check(sources.SelectedIndex == 1, "API next changes source");
                if (function == "prev") Check(sources.SelectedIndex == 0, "API prev changes source");
                if (function.StartsWith("full")) Check(Field<bool>(form, "_isFullscreen") == (function == "fullOn"), "API fullscreen command is idempotent");
            }
            const string password = "Test &+?ä=42";
            Check(Pump(form.ConfigureApiAsync(true, port, password)), "Password applies without changing port");
            using (var response = Request("next")) Check((int)response.StatusCode == 401 && sources.SelectedIndex == 0, "Missing password rejects command without switching source");
            using (var response = Request("next&Password=wrong")) Check((int)response.StatusCode == 401 && sources.SelectedIndex == 0, "Wrong password rejects command");
            using (var response = Request("next&Password=" + Uri.EscapeDataString(password))) Check(response.IsSuccessStatusCode && sources.SelectedIndex == 1, "URL-encoded password authorizes command");
            Pump(form.ConfigureApiAsync(true, port, "Changed"));
            using (var response = Request("prev&Password=" + Uri.EscapeDataString(password))) Check((int)response.StatusCode == 401, "Changing password invalidates old password immediately");
            using (var response = Request("prev&Password=changed")) Check((int)response.StatusCode == 401, "Password is case-sensitive");
            using (var response = Request("prev&Password=Changed")) Check(response.IsSuccessStatusCode, "New password works immediately");
            Pump(form.ConfigureApiAsync(true, port, ""));
            using (var response = Request("fullOFF")) Check(response.IsSuccessStatusCode, "Clearing password restores password-free access");
        }
        finally
        {
            Pump(form.ConfigureApiAsync(false, port));
            Call(form, "Disconnect");
        }
        probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
        probe.Start();
        probe.Stop();
        Check(true, "API off releases listening port");
    }

    static void TestNavigation(Form1 form)
    {
        var sources = Field<ComboBox>(form, "_cbSources");
        Call(form, "Disconnect");
        sources.Items.Clear();
        Call(form, "ChangeSource", 1);
        Check(Field<OMTReceive?>(form, "_receiver") == null, "N with no sources is harmless");
        using var first = new OMTSend("OMTplay Test A", OMTQuality.Default);
        using var second = new OMTSend("OMTplay Test B", OMTQuality.Default);
        sources.Items.AddRange(new object[] { first.URL, second.URL });
        sources.SelectedIndex = 0;
        Call(form, "ConnectSelected");
        var originalBounds = form.Bounds;
        form.ActiveControl = null;
        typeof(Form1).GetMethod("ProcessCmdKey", Private)!.Invoke(form, new object[] { new Message(), Keys.F });
        Check(Field<bool>(form, "_isFullscreen"), "F enters fullscreen");
        foreach (var (key, index) in new[] { (Keys.N, 1), (Keys.N, 0), (Keys.P, 1) })
        {
            typeof(Form1).GetMethod("ProcessCmdKey", Private)!.Invoke(form, new object[] { new Message(), key });
            Check(sources.SelectedIndex == index && Field<bool>(form, "_isFullscreen")
                && form.FormBorderStyle == FormBorderStyle.None, $"{key} switches to {index + 1}, wraps and preserves fullscreen");
        }
        typeof(Form1).GetMethod("ProcessCmdKey", Private)!.Invoke(form, new object[] { new Message(), Keys.Escape });
        Check(!Field<bool>(form, "_isFullscreen") && form.Bounds == originalBounds, "Esc restores original window bounds");
        Call(form, "Disconnect");
        sources.Items.Clear(); sources.Items.Add(first.URL); sources.SelectedIndex = 0;
        Call(form, "ConnectSelected");
        var receiver = Field<OMTReceive>(form, "_receiver");
        Call(form, "ChangeSource", 1);
        Check(ReferenceEquals(receiver, Field<OMTReceive>(form, "_receiver")), "One source does not reconnect unnecessarily");
        Call(form, "Disconnect");
    }

    static void TestNdiAndMixedSwitching()
    {
        using var discovery = new NdiDiscovery();
        NdiSource[] sources = Array.Empty<NdiSource>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (sources.Length == 0 && DateTime.UtcNow < deadline)
        {
            sources = discovery.GetSources();
            Thread.Sleep(100);
        }
        Check(sources.Length > 0, "Live NDI source discovered");
        foreach (var s in sources) Console.WriteLine("NDI source: " + s.Name);
        var source = sources.FirstOrDefault(s => s.Name.Contains("vMix", StringComparison.OrdinalIgnoreCase)) ?? sources[0];
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var receiver = new NdiReceiver(source);
            int video = 0, audio = 0, channels = 0; float peak = 0;
            deadline = DateTime.UtcNow.AddSeconds(4);
            while (DateTime.UtcNow < deadline)
                receiver.Capture(frame => { CheckFrame(frame); video++; }, (data, rate, ch, samples) =>
                { audio++; channels = ch; peak = Math.Max(peak, data.Max(Math.Abs)); });
            Check(video > 10 && audio > 10, $"NDI connection {attempt + 1}: {video} video / {audio} audio, {channels} channels, peak {peak:F4}");
        }
        using var form = new Form1();
        try
        {
            var omt = OMTDiscovery.GetInstance().GetAddresses().FirstOrDefault();
            Check(omt != null, "Live OMT source available for mixed switching");
            string ndiKey = "[NDI] " + source.Name;
            Field<Dictionary<string, NdiSource>>(form, "_ndiSources").Add(ndiKey, source);
            var list = Field<ComboBox>(form, "_cbSources");
            list.Items.AddRange(new object[] { "[OMT] " + omt, ndiKey }); list.SelectedIndex = 0;
            Call(form, "ConnectSelected");
            Call(form, "ToggleFullscreen");
            for (int i = 0; i < 4; i++)
            {
                Call(form, "ChangeSource", i % 2 == 0 ? 1 : -1);
                deadline = DateTime.UtcNow.AddSeconds(6);
                while ((Field<Bitmap?>(form, "_pendingBitmap") == null || Field<int>(form, "_receivedChannels") == 0)
                    && DateTime.UtcNow < deadline) Thread.Sleep(50);
                Check(Field<Bitmap?>(form, "_pendingBitmap") != null && Field<int>(form, "_receivedChannels") > 0
                    && Field<bool>(form, "_isFullscreen"), $"Mixed switch {i + 1}: video/audio received, fullscreen retained");
                Check(Field<bool>(form, "_audioInitDone") && !Field<bool>(form, "_audioPermanentlyDisabled"), "Shared audio playback initialized");
            }
            Call(form, "Disconnect");
            Check(Field<NdiReceiver?>(form, "_ndiReceiver") == null && Field<OMTReceive?>(form, "_receiver") == null, "Both receiver types cleanly disconnected");
        }
        finally { Call(form, "Form1_FormClosing", form, new FormClosingEventArgs(CloseReason.UserClosing, false)); }
    }

    static void TestRouting()
    {
        var route = typeof(Form1).GetMethod("RouteAudio", BindingFlags.Static | BindingFlags.NonPublic)!;
        float[] samples = Enumerable.Range(0, 24).Select(i => (i - 12) / 25f).ToArray();
        for (int selection = 0; selection < 12; selection++)
        {
            float[] result = (float[])route.Invoke(null, new object[] { samples, 8, 3, selection })!;
            int left = selection < 4 ? selection * 2 : selection - 4;
            int right = selection < 4 ? left + 1 : left;
            Check(Enumerable.Range(0, 3).All(i => result[i * 2] == samples[left * 3 + i]
                && result[i * 2 + 1] == samples[right * 3 + i]), $"Audio routing selection {selection}: correct L/R, unchanged gain");
        }
        var missing = (float[])route.Invoke(null, new object[] { new float[] { .2f, -.3f }, 1, 2, 11 })!;
        Check(missing.All(v => v == 0), "Missing selected mono channel is silent");
        var mono = (float[])route.Invoke(null, new object[] { new float[] { .2f, -.3f }, 1, 2, 4 })!;
        Check(mono.SequenceEqual(new[] { .2f, .2f, -.3f, -.3f }), "Single-channel source is duplicated to stereo");
        var invalid = (float[])route.Invoke(null, new object[] { new[] { float.NaN, float.PositiveInfinity }, 2, 1, 0 })!;
        Check(invalid.All(v => v == 0), "Non-finite samples do not reach playback or meter");
        Check(AudioLevelMeter.ToDecibels(0) == -60 && AudioLevelMeter.ToDecibels(1) == 0
            && Math.Abs(AudioLevelMeter.ToDecibels(.5f) + 6.0206f) < .001f, "Meter calibration: silence / 0 dBFS / -6.02 dBFS");
    }

    static void TestBitmap()
    {
        var convert = typeof(Form1).GetMethod("CreateBitmapFromBGRA", BindingFlags.Static | BindingFlags.NonPublic)!;
        var data = Marshal.AllocHGlobal(16);
        try
        {
            Marshal.Copy(new byte[] { 0,0,255,0, 0,0,255,0, 255,0,0,0, 255,0,0,0 }, 0, data, 16);
            var frame = new OMTMediaFrame { Data = data, DataLength = 16, Width = 2, Height = 2, Stride = 8 };
            using var bitmap = (Bitmap)convert.Invoke(null, new object[] { frame })!;
            Check(bitmap.GetPixel(0,0).R == 255 && bitmap.GetPixel(0,0).A == 255, "BGRX zero alpha remains visible");
            frame.Data = IntPtr.Add(data, 8); frame.Stride = -8;
            using var reversed = (Bitmap)convert.Invoke(null, new object[] { frame })!;
            Check(reversed.GetPixel(0,0).B == 255 && reversed.GetPixel(0,1).R == 255, "Negative stride uses valid row addresses");
            frame.DataLength = 4;
            Check(convert.Invoke(null, new object[] { frame }) == null, "Truncated video frame rejected");
        }
        finally { Marshal.FreeHGlobal(data); }
    }

    static void TestNetwork()
    {
        var discovery = OMTDiscovery.GetInstance();
        string? source = null;
        for (int i = 0; i < 30 && source == null; i++)
        {
            source = discovery.GetAddresses().FirstOrDefault(s => s.Contains("vMix"));
            if (source == null) Thread.Sleep(100);
        }
        if (source == null) { Console.WriteLine("SKIP live vMix (no source discovered)"); return; }
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using var receiver = new OMTReceive(source, OMTFrameType.Video | OMTFrameType.Audio, OMTPreferredVideoFormat.BGRA, OMTReceiveFlags.None);
            var frame = new OMTMediaFrame(); int video = 0, audio = 0; float peak = 0;
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline)
            {
                if (receiver.Receive(OMTFrameType.Video, 20, ref frame))
                {
                    CheckFrame(frame); video++;
                }
                if (receiver.Receive(OMTFrameType.Audio, 0, ref frame))
                {
                    float[] samples = new float[frame.DataLength / 4];
                    Marshal.Copy(frame.Data, samples, 0, samples.Length);
                    peak = Math.Max(peak, samples.Max(Math.Abs)); audio++;
                }
            }
            Check(video > 10 && audio > 10, $"vMix connection {attempt + 1}: {video} video / {audio} audio frames; peak {peak:F4}");
        }
    }
    static void CheckFrame(OMTMediaFrame frame)
    {
        var convert = typeof(Form1).GetMethod("CreateBitmapFromBGRA", BindingFlags.Static | BindingFlags.NonPublic)!;
        using var bitmap = (Bitmap?)convert.Invoke(null, new object[] { frame });
        if (bitmap == null) throw new Exception("Live video conversion failed");
    }
}
