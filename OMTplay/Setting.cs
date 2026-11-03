namespace OMTplay;

public partial class Setting : Form
{
    public Setting() { InitializeComponent(); }

    private void Settings_Load(object? sender, EventArgs e)
    {
        _chkStartFullscreen.Checked = Properties.Settings.Default.StartFullscreen;
        sourceNumber.Value = Math.Clamp(Properties.Settings.Default.SelectedSource, 1, 9999);
        apiEnabled.Checked = Properties.Settings.Default.ApiEnabled;
        apiPort.Value = Math.Clamp(Properties.Settings.Default.ApiPort, 1, 65535);
        apiPassword.Text = Properties.Settings.Default.ApiPassword;
        omtQuality.SelectedIndex = Properties.Settings.Default.OmtQualityPreference switch { 1 => 1, 50 => 2, 100 => 3, _ => 0 };
        ndiBandwidth.SelectedIndex = Properties.Settings.Default.NdiLowBandwidth ? 1 : 0;
    }

    private async void SaveSettings(object? sender, EventArgs e)
    {
        if (Owner is Form1 apiPlayer && !await apiPlayer.ConfigureApiAsync(apiEnabled.Checked, (int)apiPort.Value, apiPassword.Text)) return;
        Properties.Settings.Default.ApiPassword = apiPassword.Text;
        Properties.Settings.Default.ApiEnabled = apiEnabled.Checked;
        Properties.Settings.Default.ApiPort = (int)apiPort.Value;
        Properties.Settings.Default.StartFullscreen = _chkStartFullscreen.Checked;
        Properties.Settings.Default.SelectedSource = (int)sourceNumber.Value;
        Properties.Settings.Default.OmtQualityPreference = omtQuality.SelectedIndex switch { 1 => 1, 2 => 50, 3 => 100, _ => 0 };
        Properties.Settings.Default.NdiLowBandwidth = ndiBandwidth.SelectedIndex == 1;
        Properties.Settings.Default.Save();
        if (Owner is Form1 player)
            player.SetLabelText(_chkStartFullscreen.Checked ? "Fullscreenmode active" : "Fullscreenmode OFF");
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowApiPasswordChanged(object? sender, EventArgs e) =>
        apiPassword.UseSystemPasswordChar = !showApiPassword.Checked;

    private void ShowApiHelp(object? sender, EventArgs e)
    {
        using var help = new ApiHelpForm((int)apiPort.Value);
        help.ShowDialog(this);
    }
}
