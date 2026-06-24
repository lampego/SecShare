namespace SecShare.Console.Services.Archive;

public sealed record ZipArchiveBuildResult(
    byte[] ArchiveBytes,
    long SourceSizeBytes,
    int FileCount,
    string SourceName);
