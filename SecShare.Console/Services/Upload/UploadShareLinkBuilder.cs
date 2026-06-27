using SecShare.Console.Models.Upload;

namespace SecShare.Console.Services.Upload;

public static class UploadShareLinkBuilder
{
    public static UploadShareLinks Create(string fileId, string decryptionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(decryptionKey);

        var escapedFileId = Uri.EscapeDataString(fileId);
        var escapedKey = Uri.EscapeDataString(decryptionKey);
        var serviceBaseUrl = SecShareConstants.ServiceBaseUrl.TrimEnd('/');
        var link = $"{serviceBaseUrl}{SecShareConstants.ShareFilesPath}/{escapedFileId}";
        var fullSecureLink = $"{link}#{escapedKey}";

        return new UploadShareLinks(fullSecureLink, link, decryptionKey);
    }
}
