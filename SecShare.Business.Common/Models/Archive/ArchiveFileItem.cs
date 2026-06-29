namespace SecShare.Business.Common.Models.Archive;

internal sealed record ArchiveFileItem(
    string FullPath,
    string EntryName,
    long SizeBytes
);
