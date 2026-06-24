using SecShare.Console.Models.Download;

namespace SecShare.Console.Services.Download;

public sealed class SecShareDownloadLinkParser : ISecShareDownloadLinkParser
{
    public SecShareDownloadLink Parse(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var shareUri)
            || (shareUri.Scheme != Uri.UriSchemeHttps && shareUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Download URL must be an absolute HTTP or HTTPS URL.", nameof(url));
        }

        var pathSegments = shareUri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length != 2
            || !string.Equals(pathSegments[0], "f", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(pathSegments[1]))
        {
            throw new ArgumentException("Download URL must have the format '/f/{token}#key'.", nameof(url));
        }

        var encryptionKey = Uri.UnescapeDataString(shareUri.Fragment.TrimStart('#'));
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new ArgumentException("Download URL does not contain an encryption key.", nameof(url));
        }

        var token = Uri.UnescapeDataString(pathSegments[1]);
        var payloadUri = new Uri(
            SecShareConstants.ServiceBaseUri,
            $"{SecShareConstants.ApiFilesPath}/{Uri.EscapeDataString(token)}");

        return new SecShareDownloadLink(shareUri, payloadUri, encryptionKey);
    }
}
