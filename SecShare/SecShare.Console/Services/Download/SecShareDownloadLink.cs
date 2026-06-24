namespace SecShare.Console.Services.Download;

public sealed record SecShareDownloadLink(
    Uri ShareUri,
    Uri PayloadUri,
    string EncryptionKey);
