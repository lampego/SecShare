namespace SecShare.Console.Models.Archive;

public sealed record ZipArchiveBuildResult(
    byte[] ArchiveBytes,
    long SourceSizeBytes,
    int FileCount,
    string SourceName);
