using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OMTplay;

public partial class Form1
{
    private WebApplication? _api;
    private int _apiPort;
    private bool _executingApi;
    private volatile string _apiPassword = "";

    public async Task<bool> ConfigureApiAsync(bool enabled, int port, string password = "")
    {
        if (enabled && _api != null && _apiPort == port)
        {
            _apiPassword = password;
            return true;
        }
        WebApplication? replacement = null;
        if (enabled)
        {
            try
            {
                var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
                builder.Logging.ClearProviders();
                builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
                replacement = builder.Build();
                var server = replacement;
                server.MapGet("/api/", async (HttpContext context) =>
                {
                    bool PasswordMatches()
                    {
                        string expected = _apiPassword;
                        return expected.Length == 0 || string.Equals(context.Request.Query["Password"].ToString(), expected, StringComparison.Ordinal);
                    }
                    if (!PasswordMatches()) return Results.Text("Invalid or missing password.", statusCode: 401);
                    string function = context.Request.Query["Function"].ToString().ToLowerInvariant();
                    if (function is not ("prev" or "next" or "fullon" or "fulloff" or "connect"))
                        return Results.Text("Unknown Function. Use prev, next, fullOn, fullOFF or connect.", statusCode: 400);
                    var result = new TaskCompletionSource<(int Status, string Message)>(TaskCreationOptions.RunContinuationsAsynchronously);
                    try
                    {
                        BeginInvoke((Action)(() =>
                        {
                            if (_api != server || IsDisposed || context.RequestAborted.IsCancellationRequested)
                            {
                                result.TrySetResult((503, "API stopped."));
                                return;
                            }
                            if (!PasswordMatches())
                            {
                                result.TrySetResult((401, "Invalid or missing password."));
                                return;
                            }
                            try { _executingApi = true; result.TrySetResult(ExecuteApiFunction(function)); }
                            catch (Exception ex) { result.TrySetResult((500, ex.Message)); }
                            finally { _executingApi = false; }
                        }));
                        var response = await result.Task.WaitAsync(context.RequestAborted);
                        return Results.Text(response.Message, statusCode: response.Status);
                    }
                    catch (OperationCanceledException) { return Results.StatusCode(503); }
                    catch (InvalidOperationException) { return Results.Text("Player is closing.", statusCode: 503); }
                });
                await replacement.StartAsync();
            }
            catch (Exception ex)
            {
                if (replacement != null) await replacement.DisposeAsync();
                MessageBox.Show(this, $"API could not start on port {port}:\n{ex.Message}", "API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        var previous = _api;
        _apiPassword = password;
        _api = replacement;
        _apiPort = enabled ? port : 0;
        if (previous != null) await StopApiServerAsync(previous);
        return true;
    }

    private static async Task StopApiServerAsync(WebApplication server)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await server.StopAsync(timeout.Token); }
        catch (OperationCanceledException) { }
        finally { await server.DisposeAsync(); }
    }

    private (int Status, string Message) ExecuteApiFunction(string function)
    {
        switch (function)
        {
            case "prev":
            case "next":
                if (_cbSources.Items.Count == 0) return (409, "No sources available.");
                ChangeSource(function == "next" ? 1 : -1);
                return IsConnected ? (200, "OK") : (409, "Connection failed.");
            case "connect":
                if (_cbSources.SelectedIndex < 0) return (409, "No source selected.");
                if (!IsConnected || _connectedAddress != _cbSources.SelectedItem as string) ConnectSelected();
                return IsConnected ? (200, "OK") : (409, "Connection failed.");
            case "fullon":
                if (!IsConnected) return (409, "Connect a source first.");
                if (!_isFullscreen) ToggleFullscreen();
                break;
            case "fulloff":
                if (_isFullscreen) ToggleFullscreen();
                break;
        }
        return (200, "OK");
    }
}
