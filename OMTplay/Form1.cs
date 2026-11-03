using libomtnet;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OMTplay
{
    public partial class Form1 : Form
    {
        // --- OMT SDK objects (connection) ---
        private OMTDiscovery? _discovery;
        private OMTReceive? _receiver;
        private NdiDiscovery? _ndiDiscovery;
        private NdiReceiver? _ndiReceiver;
        private Dictionary<string, NdiSource> _ndiSources = new();
        private bool IsConnected => _receiver != null || _ndiReceiver != null;
        private readonly ToolTip _sourceTip = new();
        private Thread? _rxThread;
        private CancellationTokenSource? _cts;

        // --- Audio (NAudio) ---
        private IWavePlayer? _waveOut;
        private BufferedWaveProvider? _audioBuffer;
        private bool _audioInitDone = false;

        // Incoming (from OMT/vMix)
        private int _inputChannels = 2;       // actual input channels
        private int _inputSampleRate = 48000; // 48kHz

        // Output device capabilities
        private bool _deviceAcceptsFloat = true;
        private bool _audioPermanentlyDisabled = false;


        // --- Fullscreen state ---
        private bool _isFullscreen = false; // Aktueller Vollbildzustand (NICHT in Settings!)
        private FormBorderStyle _oldBorderStyle;
        private Rectangle _oldBounds;

        // 0..3: stereo pairs; 4..11: individual input channels duplicated to L/R.
        private volatile int _audioSelection;
        private int _activeAudioSelection = -1;
        private readonly object _meterLock = new();
        private readonly float[] _inputPeaks = new float[8];
        private int _meterChannels;


        private readonly System.Windows.Forms.Timer _discoveryTimer = new() { Interval = 1000 };
        private readonly System.Windows.Forms.Timer _displayTimer = new() { Interval = 15 };
        private readonly object _frameLock = new();
        private Bitmap? _pendingBitmap;
        private string _pendingResolution = "-", _pendingTimestamp = "-";
        private volatile int _receivedChannels;
        private bool _autoConnectPending;
        private bool _cursorHidden;
        private Rectangle _videoBounds;
        private DockStyle _videoDock;
        private FormWindowState _oldWindowState;
        private string? _connectedAddress;

        public void SetLabelText(string text)
        {
            label2.Text = text;
        }


        public Form1()
        {
            InitializeComponent();
            ResetBandwidth();
            _cbAudio.SelectedIndex = 0;
            _cbSources.DropDown += (_, _) => SizeSourceDropdown();
            _cbSources.SelectedIndexChanged += (_, _) => _sourceTip.SetToolTip(_cbSources, _cbSources.Text);

            _displayTimer.Tick += (_, _) => DisplayLatestFrame();
            _displayTimer.Start();
            _discoveryTimer.Tick += (_, _) => RefreshSources();
            // --- UI event assignments ---
            _btnRefresh.Click += (s, e) => RefreshSources();
            _btnConnect.Click += (s, e) => ConnectSelected();
            _btnDisconnect.Click += (s, e) => Disconnect();
            _btnFullscreen.Click += (s, e) => ToggleFullscreen();
            _btnExit.Click += (s, e) => ExitWithConfirmation();

            KeyPreview = true;

            FormClosing += Form1_FormClosing;

        }

        // --- UI: Fullscreen toggle ---
        private void ToggleFullscreen()
        {
            // Überprüfe, ob eine Quelle verbunden ist
            if (!IsConnected && !_isFullscreen)
            {
                return; // Methode abbrechen, wenn keine Verbindung besteht
            }

            // Vollbildmodus umschalten
            if (!_isFullscreen) // Verwende _isFullscreen statt Properties.Settings.Default.StartFullscreen
            {
                _isFullscreen = true; // Setze den lokalen Zustand auf true
                _oldBorderStyle = this.FormBorderStyle;
                _oldBounds = this.Bounds;
                _oldWindowState = WindowState;
                _videoBounds = _videoBox.Bounds;
                _videoDock = _videoBox.Dock;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                flowLayoutPanel1.Visible = false;
                linkLabel1.Visible = linklabel2.Visible = _audioMeter.Visible = false;
                _videoBox.Dock = DockStyle.Fill;
                _videoBox.Focus();
                Cursor.Hide();
                _cursorHidden = true;
            }
            else
            {
                _isFullscreen = false; // Setze den lokalen Zustand auf false
                FormBorderStyle = _oldBorderStyle;
                WindowState = FormWindowState.Normal;
                this.Location = _oldBounds.Location;
                this.Size = _oldBounds.Size;
                flowLayoutPanel1.Visible = true;
                linkLabel1.Visible = linklabel2.Visible = _audioMeter.Visible = true;
                flowLayoutPanel1.BringToFront();
                flowLayoutPanel1.Dock = DockStyle.Top;

                _videoBox.Dock = _videoDock;
                _videoBox.Bounds = _videoBounds;
                WindowState = _oldWindowState;
                if (_cursorHidden) { Cursor.Show(); _cursorHidden = false; }
                this.PerformLayout();


            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Text entry and open dropdowns keep their normal keyboard behaviour.
            bool editing = ActiveControl is TextBoxBase ||
                ActiveControl is ComboBox combo && (combo.DroppedDown || combo.DropDownStyle != ComboBoxStyle.DropDownList);
            if (keyData == Keys.Escape && _isFullscreen)
            {
                ToggleFullscreen();
                return true;
            }
            if (!editing)
            {
                switch (keyData)
                {
                    case Keys.N: ChangeSource(1); return true;
                    case Keys.P: ChangeSource(-1); return true;
                    case Keys.F: ToggleFullscreen(); return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ChangeSource(int direction)
        {
            _autoConnectPending = false;
            int count = _cbSources.Items.Count;
            if (count == 0) return;
            int current = _connectedAddress == null ? _cbSources.SelectedIndex : _cbSources.Items.IndexOf(_connectedAddress);
            int next = current < 0 ? (direction > 0 ? 0 : count - 1) : (current + direction + count) % count;
            string? address = _cbSources.Items[next] as string;
            _cbSources.SelectedIndex = next;
            if (IsConnected && address == _connectedAddress) return;
            ConnectSelected();
        }

        // --- Connection: Form load, OMT discovery initialization ---
        private async void Form1_Load(object? sender, EventArgs e)
        {
            Text = $"OMTplay {Application.ProductVersion.Split('+')[0]}  - Peter Aellig";
            _btnDisconnect.Visible = false;
            label2.Text = Properties.Settings.Default.StartFullscreen ? "Fullscreenmode active" : "Fullscreenmode OFF";
            _autoConnectPending = Properties.Settings.Default.StartFullscreen;
            try
            {
                _discovery = OMTDiscovery.GetInstance();

            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to initialize OMT discovery: " + ex.Message,
                    "OMT Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            try { _ndiDiscovery = new NdiDiscovery(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "NDI unavailable. OMT remains available.\n" + ex.Message,
                    "NDI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RefreshSources();
            _discoveryTimer.Start();
            await ConfigureApiAsync(Properties.Settings.Default.ApiEnabled, Properties.Settings.Default.ApiPort, Properties.Settings.Default.ApiPassword);
        }

        private void RefreshSources()
        {
            try
            {
                var selected = _cbSources.SelectedItem as string;
                if (_cbSources.DroppedDown) return;
                var ndi = _ndiDiscovery?.GetSources() ?? Array.Empty<NdiSource>();
                _ndiSources = ndi.GroupBy(x => "[NDI] " + x.Name).ToDictionary(g => g.Key, g => g.First());
                var addresses = (_discovery?.GetAddresses() ?? Array.Empty<string>())
                    .Select(x => "[OMT] " + x).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Concat(_ndiSources.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (!_cbSources.Items.Cast<string>().SequenceEqual(addresses))
                {
                    _cbSources.BeginUpdate();
                    try
                    {
                        _cbSources.Items.Clear();
                        _cbSources.Items.AddRange(addresses);
                        int index = Array.IndexOf(addresses, selected);
                        _cbSources.SelectedIndex = index >= 0 ? index : (addresses.Length > 0 ? 0 : -1);
                    }
                    finally { _cbSources.EndUpdate(); }
                }
                _lblStatus.Text = $"Discovered {addresses.Length} source(s)";
                int desired = Math.Max(1, Properties.Settings.Default.SelectedSource) - 1;
                if (_autoConnectPending && desired < addresses.Length)
                {
                    _autoConnectPending = false;
                    _cbSources.SelectedIndex = desired;
                    ConnectSelected();
                    if (IsConnected) ToggleFullscreen();
                }
            }
            catch (Exception ex) { _lblStatus.Text = "Discovery error: " + ex.Message; }
        }



        // --- Connection: Connect to selected source ---
        private void ConnectSelected()
        {
            string address = _cbSources.SelectedItem as string ?? _cbSources.Text.Trim();
            if (string.IsNullOrWhiteSpace(address) ||
                (_cbSources.SelectedItem == null && !address.StartsWith("omt://", StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, "Please select a source.");
                return;
            }

            try
            {
                DisconnectCore(preserveFullscreen: true);

                bool isNdi = _ndiSources.TryGetValue(address, out var ndiSource);
                if (isNdi) _ndiReceiver = new NdiReceiver(ndiSource!, Properties.Settings.Default.NdiLowBandwidth);
                else _receiver = new OMTReceive(
                    address.StartsWith("[OMT] ") ? address[6..] : address,
                    OMTFrameType.Video | OMTFrameType.Audio,
                    OMTPreferredVideoFormat.BGRA, OMTReceiveFlags.None);
                if (_receiver != null)
                    _receiver.SetSuggestedQuality(Properties.Settings.Default.OmtQualityPreference switch
                    {
                        1 => OMTQuality.Low,
                        50 => OMTQuality.Medium,
                        100 => OMTQuality.High,
                        _ => OMTQuality.Default
                    });

                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                var receiver = _receiver;
                _audioInitDone = false;
                _audioPermanentlyDisabled = false;
                var ndiReceiver = _ndiReceiver;
                _rxThread = new Thread(() => { if (ndiReceiver != null) ReceiveNdiLoop(ndiReceiver, token); else ReceiveLoop(receiver!, token); })
                {
                    IsBackground = true,
                    Name = "OMT Receive Thread"
                };
                _rxThread.Start();
                _connectedAddress = address;
                UpdateBandwidth();

                // UI: Update button states after connect
                _btnConnect.Enabled = false;
                _btnConnect.Visible = false;
                _btnDisconnect.Visible = true;
                _btnDisconnect.Enabled = true;

                _lblStatus.Text = "Connected: " + address;
                _lblConnected.Text = $"Connected: {address}";
                _lblResolution.Text = "-";
                _lblTimestamp.Text = "-";
                _lblConnected.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                Disconnect();
                if (!_executingApi) MessageBox.Show(this, "Connect error: " + ex.Message, "OMT Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Connection: Disconnect from source and cleanup ---
        private void Disconnect() => DisconnectCore(preserveFullscreen: false);

        private void DisconnectCore(bool preserveFullscreen)
        {
            _autoConnectPending = false;
            if (_isFullscreen && !preserveFullscreen) ToggleFullscreen();
            try
            {
                _cts?.Cancel();
                // Receive uses a bounded timeout and never waits for the UI thread.
                if (_rxThread is { IsAlive: true }) _rxThread.Join();
            }
            catch (Exception ex) { Debug.WriteLine("Thread stop error: " + ex); }
            finally
            {
                _rxThread = null;
                _cts?.Dispose();
                _cts = null;
            }

            // WICHTIG: Receiver ZUERST stoppen!
            try { _receiver?.Dispose(); }
            catch { }
            finally { _receiver = null; _connectedAddress = null; }
            try { _ndiReceiver?.Dispose(); }
            finally { _ndiReceiver = null; }

            // Audio KOMPLETT zurücksetzen
            try
            {
                _waveOut?.Stop();

                _waveOut?.Dispose();
            }
            catch { }
            _waveOut = null;
            _audioBuffer = null;
            _audioInitDone = false;
            _audioPermanentlyDisabled = false; // WICHTIG!

            lock (_frameLock) { _pendingBitmap?.Dispose(); _pendingBitmap = null; }
            _receivedChannels = 0;
            _activeAudioSelection = -1;
            lock (_meterLock) { Array.Clear(_inputPeaks); _meterChannels = 0; }
            _audioMeter.ResetLevels();
            // Video-Image zurücksetzen
            if (_videoBox.Image != null)
            {
                var oldImage = _videoBox.Image;
                _videoBox.Image = null;
                oldImage.Dispose();
            }

            // UI zurücksetzen
            _btnConnect.Enabled = true;
            _btnConnect.Visible = true;
            _btnDisconnect.Visible = false;
            _btnDisconnect.Enabled = false;

            _lblConnected.Text = "Disconnected";
            _lblConnected.ForeColor = Color.Red;
            _lblResolution.Text = "Resolution";
            _lblTimestamp.Text = "Timestamp";
            _lblAudiochannels.Text = "Audio Channels (max.8)";
            ResetBandwidth();
        }

        // Try complete initialization: devices can fail in Init/Play, not just construction.
        private void EnsureAudioInit(int sampleRate, int channels)
        {
            if (_audioPermanentlyDisabled) return;
            if (_audioInitDone && sampleRate == _inputSampleRate && channels == _inputChannels) return;
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _audioBuffer = null;
            _audioInitDone = false;
            _inputSampleRate = sampleRate;
            _inputChannels = channels;
            int outChannels = 2; // Mono selection is always sent to both stereo outputs.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                IWavePlayer? player = null;
                try
                {
                    bool useFloat = attempt == 0;
                    var format = useFloat ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outChannels)
                        : new WaveFormat(sampleRate, 16, outChannels);
                    var buffer = new BufferedWaveProvider(format)
                    {
                        BufferDuration = TimeSpan.FromMilliseconds(250),
                        DiscardOnBufferOverflow = true,
                        ReadFully = true
                    };
                    player = attempt < 2 ? new WasapiOut(AudioClientShareMode.Shared, false, 100)
                        : new WaveOutEvent { DesiredLatency = 150 };
                    player.Init(buffer);
                    player.Play();
                    _waveOut = player;
                    _audioBuffer = buffer;
                    _deviceAcceptsFloat = useFloat;
                    _audioInitDone = true;
                    return;
                }
                catch (Exception ex) { player?.Dispose(); Debug.WriteLine("Audio init: " + ex); }
            }
            _audioPermanentlyDisabled = true;
        }

        private void ProcessAudio(OMTMediaFrame frame)
        {
            if (frame.Data == IntPtr.Zero || frame.Channels < 1 || frame.Channels > 32 ||
                frame.SampleRate < 8000 || frame.SampleRate > 192000 || frame.SamplesPerChannel <= 0) return;
            int count = checked(frame.Channels * frame.SamplesPerChannel);
            if (frame.DataLength < checked(count * sizeof(float))) return;
            float[] planar = new float[count];
            Marshal.Copy(frame.Data, planar, 0, count);
            ProcessPlanarAudio(planar, frame.SampleRate, frame.Channels, frame.SamplesPerChannel);
        }

        private void ProcessPlanarAudio(float[] planar, int sampleRate, int channels, int samples)
        {
            _receivedChannels = channels;
            // Meter the original input channels, independent of the listening selection/device.
            lock (_meterLock)
            {
                int count = Math.Min(8, channels);
                if (_meterChannels != count) Array.Clear(_inputPeaks);
                _meterChannels = count;
                for (int channel = 0; channel < count; channel++)
                    for (int sample = 0; sample < samples; sample++)
                    {
                        float value = planar[channel * samples + sample];
                        if (float.IsFinite(value))
                            _inputPeaks[channel] = Math.Max(_inputPeaks[channel], Math.Abs(value));
                    }
            }
            EnsureAudioInit(sampleRate, channels);
            if (_audioBuffer == null) return;
            int selection = _audioSelection; // Snapshot once per packet, so L/R change together.
            if (_activeAudioSelection != selection)
            {
                _audioBuffer.ClearBuffer();
                _activeAudioSelection = selection;
            }
            var interleaved = RouteAudio(planar, channels, samples, selection);
            byte[] bytes = new byte[interleaved.Length * (_deviceAcceptsFloat ? 4 : 2)];
            if (_deviceAcceptsFloat) Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);
            else FloatInterleavedToPcm16Bytes(interleaved, bytes);
            _audioBuffer.AddSamples(bytes, 0, bytes.Length);
        }


        // Convert float [-1..1] -> 16-bit PCM
        private static void FloatInterleavedToPcm16Bytes(float[] interleaved, byte[] dest)
        {
            int di = 0;
            for (int i = 0; i < interleaved.Length; i++)
            {
                float f = interleaved[i];
                if (f > 1f) f = 1f; else if (f < -1f) f = -1f;
                short s = (short)(f * 32767f);
                dest[di++] = (byte)(s & 0xFF);
                dest[di++] = (byte)((s >> 8) & 0xFF);
            }
        }

        private void AudioSelectionChanged(object? sender, EventArgs e)
        {
            if (_cbAudio.SelectedIndex < 0) return;
            _audioSelection = _cbAudio.SelectedIndex;
        }

        private static float[] RouteAudio(float[] planar, int channels, int samplesPerChannel, int selection)
        {
            if (selection < 0 || selection > 11) throw new ArgumentOutOfRangeException(nameof(selection));
            bool mono = selection >= 4;
            int left = mono ? selection - 4 : selection * 2;
            int right = mono ? left : left + 1;
            var result = new float[checked(samplesPerChannel * 2)];
            for (int i = 0; i < samplesPerChannel; i++)
            {
                // Missing channels are silent. Mono is copied without summing or boosting gain.
                float l = left < channels ? planar[left * samplesPerChannel + i] : 0;
                float r = right < channels ? planar[right * samplesPerChannel + i] : 0;
                result[i * 2] = float.IsFinite(l) ? l : 0;
                result[i * 2 + 1] = float.IsFinite(r) ? r : 0;
            }
            return result;
        }

        // --- Main receive loop: handles audio and video frames ---
        private void ReceiveLoop(OMTReceive receiver, CancellationToken ct)
        {
            var frame = new OMTMediaFrame();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Keep draining audio even if the machine has no audio output device.
                    for (int i = 0; i < 32 && !ct.IsCancellationRequested &&
                        receiver.Receive(OMTFrameType.Audio, 0, ref frame); i++)
                    {
                        try { ProcessAudio(frame); }
                        catch (Exception ex) { Debug.WriteLine("Audio: " + ex); _audioPermanentlyDisabled = true; }
                    }
                    if (ct.IsCancellationRequested) break;
                    if (receiver.Receive(OMTFrameType.Video, 20, ref frame))
                    {
                        QueueVideoFrame(frame);
                    }
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Debug.WriteLine("Receive: " + ex);
                        try
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if (ct.IsCancellationRequested) return;
                                Disconnect();
                                MessageBox.Show(this, "Receive error: " + ex.Message, "OMT Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                        catch (InvalidOperationException) { }
                    }
                    break;
                }
            }
        }

        private void QueueVideoFrame(OMTMediaFrame frame)
        {
            var bitmap = CreateBitmapFromBGRA(frame);
            if (bitmap == null) return;
            lock (_frameLock)
            {
                _pendingBitmap?.Dispose();
                _pendingBitmap = bitmap;
                _pendingResolution = FormatResolution(frame);
                _pendingTimestamp = FormatTimestamp(frame.Timestamp, frame.FrameRateN, frame.FrameRateD);
            }
        }

        private void ReceiveNdiLoop(NdiReceiver receiver, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                    receiver.Capture(QueueVideoFrame, (data, rate, channels, samples) =>
                    {
                        try { ProcessPlanarAudio(data, rate, channels, samples); }
                        catch (Exception ex) { Debug.WriteLine(ex); _audioPermanentlyDisabled = true; }
                    });
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        Disconnect();
                        MessageBox.Show(this, ex.Message, "NDI receive error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                catch (InvalidOperationException) { }
            }
        }

        private void DisplayLatestFrame()
        {
            UpdateBandwidth();
            Span<float> peaks = stackalloc float[8];
            int channels;
            lock (_meterLock)
            {
                _inputPeaks.AsSpan().CopyTo(peaks);
                channels = _meterChannels;
                Array.Clear(_inputPeaks);
            }
            _audioMeter.UpdatePeaks(peaks, channels);
            lock (_frameLock)
            {
                if (_pendingBitmap != null)
                {
                    var old = _videoBox.Image;
                    _videoBox.Image = _pendingBitmap;
                    _pendingBitmap = null;
                    _lblResolution.Text = _pendingResolution;
                    _lblTimestamp.Text = _pendingTimestamp;
                    old?.Dispose();
                }
            }
            if (_receivedChannels > 0)
                _lblAudiochannels.Text = $"Received audio channels: {_receivedChannels}";
        }


        // --- Video: Convert BGRA frame to Bitmap (resistant to negative stride) ---
        private static Bitmap? CreateBitmapFromBGRA(OMTMediaFrame frame)
        {
            if (frame.Data == IntPtr.Zero || frame.Width <= 0 || frame.Height <= 0)
                return null;

            try
            {
                int rows = frame.Height;
                int srcStride = frame.Stride;
                int absSrcStride = Math.Abs(srcStride);

                int rowBytes = checked(frame.Width * 4);
                if (absSrcStride < rowBytes || frame.DataLength < (long)absSrcStride * rows) return null;

                var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppRgb);
                var rect = new Rectangle(0, 0, frame.Width, frame.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

                try
                {
                    int dstStride = bmpData.Stride;
                    int bytesPerRow = rowBytes;
                    byte[] rowBuf = new byte[bytesPerRow];

                    for (int y = 0; y < rows; y++)
                    {
                        IntPtr srcRow = IntPtr.Add(frame.Data, y * srcStride);
                        IntPtr dstRow = IntPtr.Add(bmpData.Scan0, y * dstStride);

                        Marshal.Copy(srcRow, rowBuf, 0, bytesPerRow);
                        Marshal.Copy(rowBuf, 0, dstRow, bytesPerRow);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("Bitmap error: " + ex);
                return null;
            }
        }

        // --- Connection: Cleanup on form closing ---
        private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            var api = _api;
            _api = null;
            _discoveryTimer.Stop();
            _displayTimer.Stop();
            Disconnect();
            _ndiDiscovery?.Dispose();
            _sourceTip.Dispose();
            _discoveryTimer.Dispose();
            _displayTimer.Dispose();
            if (api != null) await StopApiServerAsync(api);
        }

        // --- UI: Exit confirmation dialog ---
        private void ExitWithConfirmation()
        {
            var result = MessageBox.Show(this, "Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) Close();
        }

        // --- Video: Format timestamp for display ---
        private static string FormatTimestamp(long timestamp, int frameRateN, int frameRateD)
        {
            // 1 second = 10,000,000 ticks
            long totalSeconds = timestamp / 10_000_000;
            long remainder = timestamp % 10_000_000;

            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)(totalSeconds % 60);

            double frameRate = frameRateD != 0 ? (double)frameRateN / frameRateD : 25.0; // fallback 25fps
            int frame = (int)(remainder * frameRate / 10_000_000);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frame:D2}";
        }

        // --- Video: Format resolution and framerate for display ---
        private static string FormatResolution(OMTMediaFrame frame)
        {
            double fps = frame.FrameRateD != 0 ? (double)frame.FrameRateN / frame.FrameRateD : 0;
            string res = $"{frame.Height}p";
            string fpsStr = fps > 0 ? $"{Math.Round(fps)}" : "?";
            return $"{res}{fpsStr}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using Setting settingForm = new Setting();
            settingForm.ShowDialog(this);
        }


    }
}
