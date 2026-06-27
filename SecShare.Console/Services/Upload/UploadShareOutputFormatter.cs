using SecShare.Console.Enums;
using SecShare.Console.Models.Upload;
using SecShare.Console.Ui;

namespace SecShare.Console.Services.Upload;

public static class UploadShareOutputFormatter
{
    public static string Format(
        UploadPackage package,
        UploadShareLinks links,
        string mode,
        string expires,
        int downloads
    )
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(links);

        return string.Join(
            Environment.NewLine,
            CreateLines(package, links, mode, expires, downloads)
                .Select(line => string.Concat(line.Segments.Select(segment => segment.Text)))
        );
    }

    public static IReadOnlyList<UploadShareOutputLine> CreateLines(
        UploadPackage package,
        UploadShareLinks links,
        string mode,
        string expires,
        int downloads
    )
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(links);

        var size = $"{TransferProgressUi.FormatBytes(package.SourceSizeBytes)}"
            + $" -> {TransferProgressUi.FormatBytes(package.EncryptedPayload.LongLength)} encrypted";

        return
        [
            Line(("Upload completed", UploadShareOutputStyle.Success)),
            Line(
                ("Details: ", UploadShareOutputStyle.Header),
                ("Source: ", UploadShareOutputStyle.Label),
                (package.SourceName, UploadShareOutputStyle.Value),
                (" | Mode: ", UploadShareOutputStyle.Label),
                (mode, UploadShareOutputStyle.Value),
                (" | Files: ", UploadShareOutputStyle.Label),
                (package.FileCount.ToString(), UploadShareOutputStyle.Value)
            ),
            Line(
                ("Size: ", UploadShareOutputStyle.Label),
                (size, UploadShareOutputStyle.Value),
                (" | Expires: ", UploadShareOutputStyle.Label),
                (expires, UploadShareOutputStyle.Value),
                (" | Downloads: ", UploadShareOutputStyle.Label),
                (downloads.ToString(), UploadShareOutputStyle.Value)
            ),
            Line(("Download command:", UploadShareOutputStyle.Header)),
            Line(($"secshare get \"{links.FullSecureLink}\"", UploadShareOutputStyle.Value)),
            Line(("Sharing options:", UploadShareOutputStyle.Header)),
            Line(
                ("1. Full secure link", UploadShareOutputStyle.Header),
                (" - Anyone with this link can decrypt and download the file.", UploadShareOutputStyle.Secondary)
            ),
            Line((links.FullSecureLink, UploadShareOutputStyle.Value)),
            Line(
                ("2. Separate link and decryption key", UploadShareOutputStyle.Header),
                (" - Send the link in chat. Share the key separately.", UploadShareOutputStyle.Secondary)
            ),
            Line(("Link:", UploadShareOutputStyle.Label)),
            Line((links.Link, UploadShareOutputStyle.Value)),
            Line(("Decryption key:", UploadShareOutputStyle.Label)),
            Line((links.DecryptionKey, UploadShareOutputStyle.Value)),
        ];
    }

    private static UploadShareOutputLine Line(
        params (string Text, UploadShareOutputStyle Style)[] segments
    )
        => new(segments.Select(segment => new UploadShareOutputSegment(segment.Text, segment.Style)).ToArray());
}
