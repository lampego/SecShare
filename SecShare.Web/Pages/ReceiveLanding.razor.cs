using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace SecShare.Web.Pages;

public partial class ReceiveLanding
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private string _linkInput = string.Empty;
    private string? _error;

    private async Task OnInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OpenLink();
        }
    }

    private async Task OpenLink()
    {
        _error = null;

        var raw = _linkInput.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            _error = "Please paste a SecShare link.";
            return;
        }

        if (!TryParseReceiveUrl(raw, out var targetUrl))
        {
            _error = "This does not look like a valid SecShare link. It should look like: https://secshare.me/f/…";
            return;
        }

        // Use JS navigation so the URL fragment (#key) is preserved in window.location.hash
        // and can be read by the Receive page without being sent to the server.
        await JS.InvokeVoidAsync("secshareInterop.navigateTo", targetUrl);
    }

    /// <summary>
    /// Parses a pasted SecShare URL and builds a same-origin target URL
    /// that preserves the path and fragment.
    /// Accepts both full URLs (https://secshare.me/f/…#key) and bare paths (/f/…#key).
    /// </summary>
    private static bool TryParseReceiveUrl(string raw, out string targetUrl)
    {
        targetUrl = string.Empty;

        // Try parsing as a full URL first.
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
        {
            var segments = absolute.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 2
                && string.Equals(segments[0], "f", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(segments[1]))
            {
                // Rebuild as relative URL so we stay on the current origin.
                targetUrl = $"/f/{Uri.EscapeDataString(Uri.UnescapeDataString(segments[1]))}{absolute.Fragment}";
                return true;
            }

            return false;
        }

        // Try parsing as a relative path like /f/{id}#key.
        if (Uri.TryCreate(raw, UriKind.Relative, out _))
        {
            // Use a dummy base so we can parse the path segments.
            if (Uri.TryCreate(new Uri("https://secshare.me"), raw, out var combined))
            {
                var segments = combined.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 2
                    && string.Equals(segments[0], "f", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(segments[1]))
                {
                    targetUrl = $"/f/{Uri.EscapeDataString(Uri.UnescapeDataString(segments[1]))}{combined.Fragment}";
                    return true;
                }
            }
        }

        return false;
    }
}

