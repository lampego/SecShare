using SecShare.Console.Models.Upload;

namespace SecShare.Console.Services.Upload;

public static class UploadShareOutputFormatter
{
    public static string Format(UploadShareLinks links)
    {
        ArgumentNullException.ThrowIfNull(links);

        return
            $"""
            Full secure link:
            {links.FullSecureLink}

            Anyone with this full link can decrypt and download the file.

            Separate sharing:
            Link:
            {links.Link}

            Decryption key:
            {links.DecryptionKey}

            You can send the link in chat and share the decryption key separately for extra safety.
            """;
    }
}
