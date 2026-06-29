using SecShare.Console.Models.Download;

namespace SecShare.Console.Services.Download;

public sealed class SecShareDownloadLinkParser : ISecShareDownloadLinkParser
{
    public SecShareDownloadLink Parse(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var shareUri)
            || (shareUri.Scheme != Uri.UriSchemeHttps && shareUri.Scheme != Uri.UriSchemeHttp)
        )
        {
            throw new ArgumentException("Download URL must be an absolute HTTP or HTTPS URL.", nameof(url));
        }

        var pathSegments = shareUri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length != 2
            || !string.Equals(pathSegments[0], "f", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(pathSegments[1])
        )
        {
            throw new ArgumentException("Download URL must have the format '/f/{fileId}' with an optional '#key' fragment.", nameof(url));
        }

        var encryptionKey = shareUri.Fragment.Length == 0
            ? null
            : Uri.UnescapeDataString(shareUri.Fragment.TrimStart('#'));
        var fileId = Uri.UnescapeDataString(pathSegments[1]);
        var baseUri = new Uri(shareUri.GetLeftPart(UriPartial.Authority));
        var payloadUri = new Uri(
            baseUri,
            $"{SecShareConstants.ApiFilesPath}/{Uri.EscapeDataString(fileId)}"
        );

        return new SecShareDownloadLink(shareUri, payloadUri, fileId, encryptionKey);
    }
}
