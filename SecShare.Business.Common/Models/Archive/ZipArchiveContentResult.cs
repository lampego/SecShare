namespace SecShare.Business.Common.Models.Archive;

public sealed record ZipArchiveContentResult(
    string? TextContent,
    byte[]? FileBytes,
    string? FileName
)
{
    public bool IsText
        => TextContent is not null;
}
