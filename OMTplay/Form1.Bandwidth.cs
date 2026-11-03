using System.Diagnostics;

namespace OMTplay;

public partial class Form1
{
    private long _bandwidthTimestamp;
    private long _bandwidthBytes;

    private void ResetBandwidth()
    {
        _bandwidthTimestamp = Stopwatch.GetTimestamp();
        _bandwidthBytes = 0;
        lblbwidth.Text = "Bandwidth";
        _sourceTip.SetToolTip(lblbwidth, "Received video + audio data rate in Mbit/s (OMT SDK statistics; excludes network overhead).");
    }

    private void UpdateBandwidth()
    {
        if (!IsConnected) return;
        if (_ndiReceiver != null)
        {
            lblbwidth.Text = "Bandwidth: n/a";
            _sourceTip.SetToolTip(lblbwidth, "The NDI SDK used by OMTplay does not expose received network byte counts.");
            return;
        }
        long now = Stopwatch.GetTimestamp();
        double seconds = Stopwatch.GetElapsedTime(_bandwidthTimestamp, now).TotalSeconds;
        if (seconds < 1 || _receiver == null) return;
        try
        {
            long bytes = _receiver.GetVideoStatistics().BytesReceived + _receiver.GetAudioStatistics().BytesReceived;
            long delta = Math.Max(0, bytes - _bandwidthBytes);
            lblbwidth.Text = $"{delta * 8d / seconds / 1_000_000:F1} Mbit/s";
            _bandwidthBytes = bytes;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Bandwidth statistics: " + ex.Message);
            lblbwidth.Text = "Bandwidth: n/a";
        }
        _bandwidthTimestamp = now;
    }
}
