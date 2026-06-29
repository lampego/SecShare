namespace SecShare.Console.Models.Archive;

internal sealed record ArchiveFileItem(
    string FullPath,
    string EntryName,
    long SizeBytes
);
