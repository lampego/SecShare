namespace SecShare.Console.Models.Download;

public sealed record SecShareDownloadLink(
    Uri ShareUri,
    Uri PayloadUri,
    string FileId,
    string? EncryptionKey);
