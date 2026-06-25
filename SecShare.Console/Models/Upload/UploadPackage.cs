namespace SecShare.Console.Models.Upload;

public sealed record UploadPackage(
    byte[] EncryptedPayload,
    string EncryptionKey,
    long SourceSizeBytes,
    long ArchiveSizeBytes,
    int FileCount,
    string SourceName);
