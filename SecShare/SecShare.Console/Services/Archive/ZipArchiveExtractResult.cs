namespace SecShare.Console.Services.Archive;

public sealed record ZipArchiveExtractResult(
    IReadOnlyCollection<string> ExtractedPaths,
    long ExtractedSizeBytes,
    int FileCount);
