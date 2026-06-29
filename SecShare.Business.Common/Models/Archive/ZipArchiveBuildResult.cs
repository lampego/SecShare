namespace SecShare.Business.Common.Models.Archive;

public sealed record ZipArchiveBuildResult(
    byte[] ArchiveBytes,
    long SourceSizeBytes,
    int FileCount,
    string SourceName
);
