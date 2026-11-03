using System.Diagnostics;
using System.ComponentModel;

namespace OMTplay;

/// <summary>Vertical input sample-peak meters for up to eight received channels, calibrated in dBFS.</summary>
public sealed class AudioLevelMeter : System.Windows.Forms.Control
{
    private readonly float[] levels = Enumerable.Repeat(-60f, 8).ToArray();
    private readonly float[] holds = Enumerable.Repeat(-60f, 8).ToArray();
    private readonly long[] holdUntil = new long[8];
    private readonly long[] clipUntil = new long[8];
    private int activeChannels;
    private long lastUpdate = Stopwatch.GetTimestamp();
    private Color lowLevelColor = Color.LimeGreen, warningColor = Color.Gold, highLevelColor = Color.Red;
    private Color barBackgroundColor = Color.Black, peakHoldColor = Color.White;
    private float warningThreshold = -18, highThreshold = -6;
    private int barSpacing = 3, peakHoldMilliseconds = 1000;
    private bool showScale = true, showPeakHold = true;

    [Category("Audiometer"), Description("Farbe unterhalb des Warnpegels."), DefaultValue(typeof(Color), "LimeGreen")]
    public Color LowLevelColor { get => lowLevelColor; set { lowLevelColor = value; Invalidate(); } }

    [Category("Audiometer"), Description("Farbe ab WarningThresholdDb."), DefaultValue(typeof(Color), "Gold")]
    public Color WarningColor { get => warningColor; set { warningColor = value; Invalidate(); } }

    [Category("Audiometer"), Description("Farbe ab HighThresholdDb und für die Übersteuerungsanzeige."), DefaultValue(typeof(Color), "Red")]
    public Color HighLevelColor { get => highLevelColor; set { highLevelColor = value; Invalidate(); } }

    [Category("Audiometer"), Description("Hintergrund der einzelnen Pegelbalken."), DefaultValue(typeof(Color), "Black")]
    public Color BarBackgroundColor { get => barBackgroundColor; set { barBackgroundColor = value; Invalidate(); } }

    [Category("Audiometer"), Description("Farbe der Peak-Hold-Linie."), DefaultValue(typeof(Color), "White")]
    public Color PeakHoldColor { get => peakHoldColor; set { peakHoldColor = value; Invalidate(); } }

    [Category("Audiometer"), Description("Warnpegel in dBFS (-60 bis 0). Oberhalb des Hochpegels hat dessen Farbe Vorrang."), DefaultValue(-18f)]
    public float WarningThresholdDb
    {
        get => warningThreshold;
        set { ValidateThreshold(value); warningThreshold = value; Invalidate(); }
    }

    [Category("Audiometer"), Description("Hochpegel in dBFS (-60 bis 0)."), DefaultValue(-6f)]
    public float HighThresholdDb
    {
        get => highThreshold;
        set { ValidateThreshold(value); highThreshold = value; Invalidate(); }
    }

    [Category("Audiometer"), Description("Abstand zwischen den Balken in Pixeln bei 96 DPI."), DefaultValue(3)]
    public int BarSpacing
    {
        get => barSpacing;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); barSpacing = value; Invalidate(); }
    }

    [Category("Audiometer"), Description("Haltezeit der Pegelspitze in Millisekunden."), DefaultValue(1000)]
    public int PeakHoldMilliseconds
    {
        get => peakHoldMilliseconds;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); peakHoldMilliseconds = value; Array.Clear(holdUntil); Invalidate(); }
    }

    [Category("Audiometer"), Description("Peak-Hold-Linie anzeigen."), DefaultValue(true)]
    public bool ShowPeakHold { get => showPeakHold; set { showPeakHold = value; Invalidate(); } }

    [Category("Audiometer"), Description("dBFS-Skala anzeigen. Ohne Skala nutzen die Balken die gesamte Breite."), DefaultValue(true)]
    public bool ShowScale { get => showScale; set { showScale = value; Invalidate(); } }

    private static void ValidateThreshold(float value)
    {
        if (!float.IsFinite(value) || value < -60 || value > 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Der Pegel muss zwischen -60 und 0 dBFS liegen.");
    }

    public AudioLevelMeter()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.Silver;
        Size = new Size(180, 400);
        AccessibleName = "Input audio channels 1 to 8, dBFS";
        TabStop = false;
    }

    public void ResetLevels()
    {
        Array.Fill(levels, -60);
        Array.Fill(holds, -60);
        Array.Clear(holdUntil);
        Array.Clear(clipUntil);
        activeChannels = 0;
        lastUpdate = Stopwatch.GetTimestamp();
        Invalidate();
    }

    public static float ToDecibels(float peak) =>
        float.IsFinite(peak) && peak > 0 ? Math.Clamp(20 * MathF.Log10(peak), -60, 0) : -60;

    public void UpdatePeaks(ReadOnlySpan<float> peaks, int channelCount)
    {
        long now = Stopwatch.GetTimestamp();
        float elapsed = (float)Stopwatch.GetElapsedTime(lastUpdate, now).TotalSeconds;
        lastUpdate = now;
        int count = Math.Clamp(channelCount, 0, Math.Min(8, peaks.Length));
        for (int channel = 0; channel < 8; channel++)
        {
            if (channel >= count || channel >= activeChannels)
            {
                levels[channel] = holds[channel] = -60;
                holdUntil[channel] = clipUntil[channel] = 0;
            }
            if (channel < count) UpdateChannel(channel, peaks[channel], now, elapsed);
        }
        activeChannels = count;
        Invalidate();
    }

    private void UpdateChannel(int channel, float peak, long now, float elapsed)
    {
        float db = ToDecibels(peak);
        levels[channel] = Math.Max(db, Math.Max(-60, levels[channel] - elapsed * 30));
        if (db >= holds[channel])
        {
            holds[channel] = db;
            holdUntil[channel] = now + (long)(Stopwatch.Frequency * (peakHoldMilliseconds / 1000d));
        }
        else if (now > holdUntil[channel]) holds[channel] = Math.Max(levels[channel], holds[channel] - elapsed * 20);
        if (peak >= 1) clipUntil[channel] = now + Stopwatch.Frequency;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        bool preview = DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        int visibleChannels = preview ? 8 : activeChannels;
        if (visibleChannels == 0) return;
        float scale = DeviceDpi / 96f;
        int scaleWidth = showScale ? (int)(30 * scale) : 0;
        int header = Math.Max(Font.Height + 5, (int)(22 * scale));
        int bottom = Height - Font.Height - (int)(7 * scale);
        int barHeight = bottom - header;
        float columnWidth = (Width - scaleWidth - 3) / (float)visibleChannels;
        if (barHeight < 4 || columnWidth < 3) return;
        if (showScale) TextRenderer.DrawText(e.Graphics, "dBFS", Font, new Point(0, 0), ForeColor);
        foreach (int db in showScale ? new[] { 0, -6, -12, -18, -24, -36, -48, -60 } : Array.Empty<int>())
        {
            int y = bottom - (int)((db + 60) / 60f * barHeight);
            TextRenderer.DrawText(e.Graphics, db.ToString(), Font,
                new Rectangle(0, y - Font.Height / 2, scaleWidth - 2, Font.Height), ForeColor,
                TextFormatFlags.Right | TextFormatFlags.NoPadding);
        }
        long now = Stopwatch.GetTimestamp();
        using var lowBrush = new SolidBrush(lowLevelColor);
        using var warningBrush = new SolidBrush(warningColor);
        using var highBrush = new SolidBrush(highLevelColor);
        using var backgroundBrush = new SolidBrush(barBackgroundColor);
        using var peakPen = new Pen(peakHoldColor);
        for (int channel = 0; channel < visibleChannels; channel++)
        {
            int x = scaleWidth + (int)(channel * columnWidth);
            int width = Math.Max(1, (int)(columnWidth - barSpacing * scale));
            bool active = preview || channel < activeChannels;
            var bar = new Rectangle(x, header, width, barHeight);
            e.Graphics.FillRectangle(backgroundBrush, bar);
            float level = preview ? -3 - channel * 5 : levels[channel];
            float heldLevel = preview ? Math.Min(0, level + 3) : holds[channel];
            int fill = (int)((level + 60) / 60 * bar.Height);
            for (int offset = 0; active && offset < fill; offset += Math.Max(2, (int)(4 * scale)))
            {
                float db = -60 + 60f * offset / bar.Height;
                var brush = db >= highThreshold ? highBrush : db >= warningThreshold ? warningBrush : lowBrush;
                int segment = Math.Min(Math.Max(1, (int)(3 * scale)), fill - offset);
                e.Graphics.FillRectangle(brush, bar.Left, bar.Bottom - offset - segment, bar.Width, segment);
            }
            if (showPeakHold && active && heldLevel > -60)
            {
                int y = bar.Bottom - 1 - Math.Min(bar.Height - 1, (int)((heldLevel + 60) / 60 * bar.Height));
                e.Graphics.DrawLine(peakPen, bar.Left, y, bar.Right - 1, y);
            }
            e.Graphics.FillRectangle(active && now < clipUntil[channel] ? highBrush : Brushes.DimGray,
                x, header - (int)(6 * scale), width, Math.Max(2, (int)(3 * scale)));
            TextRenderer.DrawText(e.Graphics, (channel + 1).ToString(), Font,
                new Rectangle(x - 1, bottom + 3, (int)columnWidth, Font.Height), active ? ForeColor : Color.DimGray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
        }
    }
}
