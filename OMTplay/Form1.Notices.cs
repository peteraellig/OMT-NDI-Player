using System.Diagnostics;

namespace OMTplay;

public partial class Form1
{
    private void linkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        OpenLicenseLink(Path.Combine(AppContext.BaseDirectory, "licenses", "OMT-LICENSE.txt"));

    private void linklabel2_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) =>
        OpenLicenseLink("https://ndi.video/");

    private void OpenLicenseLink(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Open link", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }

    private void SizeSourceDropdown()
    {
        int width = Math.Max(_cbSources.Width, 600 * DeviceDpi / 96);
        foreach (var item in _cbSources.Items)
            width = Math.Max(width, TextRenderer.MeasureText(item.ToString(), _cbSources.Font).Width + SystemInformation.VerticalScrollBarWidth + 24);
        _cbSources.DropDownWidth = Math.Min(width, Screen.FromControl(this).WorkingArea.Width - 32);
    }
}
