namespace SecShare.Console.Models.Archive;

public sealed record ZipArchiveExtractResult(
    IReadOnlyCollection<string> ExtractedPaths,
    long ExtractedSizeBytes,
    int FileCount
);
