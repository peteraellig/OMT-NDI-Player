namespace OMTplay;

public partial class ApiHelpForm : Form
{
    public ApiHelpForm() : this(25901) { }

    public ApiHelpForm(int port)
    {
        InitializeComponent();
        helpText.Text = $"""
            OMTplay HTTP API

            In Settings, select "API enabled (network)", set the port and click SAVE. Changes take effect immediately. The API listens on all network interfaces while OMTplay is running and the API is enabled.

            REQUEST (HTTP GET)
            On this PC:
            http://127.0.0.1:{port}/api/?Function=next

            From another device, replace 127.0.0.1 with the IP address of the OMTplay PC. The port must be accessible over the network.

            COMMANDS
            prev       Select and connect the previous source.
            next       Select and connect the next source.
                       Navigation wraps around at either end of the list.
                       Fullscreen remains active when switching sources.
            fullOn     Enter fullscreen (a source must be connected).
            fullOFF    Leave fullscreen.
                       Repeating either command does not toggle the state.
            connect    Connect the source selected in the source dropdown.
                       If that source is already connected, it stays connected.

            Example:
            http://127.0.0.1:{port}/api/?Function=fullOn

            PASSWORD (OPTIONAL)
            Enter a password in Settings and click SAVE. Select "Show" to reveal the password. Leave the field empty to disable password checking.

            http://127.0.0.1:{port}/api/?Function=next&Password=MyPassword

            After the first ?, append additional parameters using &. Passwords are case-sensitive. URL-encode special characters: for example, & as %26, + as %2B and spaces as %20. The password appears in the URL and is sent unencrypted over HTTP.

            RESPONSES
            200 / OK   Command completed.
            400        Unknown or missing Function.
            401        Missing or incorrect password.
            409        No suitable source or connection available.
            500        Command execution failed.
            503        The API is stopping or is no longer active.

            When the API is disabled, the port does not accept requests.
            """.Replace("\n", "\r\n");
    }
}
