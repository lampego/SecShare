namespace SecShare.Business.Common.Models.Archive;

public sealed record ZipArchiveSourceItem(
    string EntryName,
    long SizeBytes,
    Func<CancellationToken, ValueTask<Stream>> OpenReadStreamAsync
);
