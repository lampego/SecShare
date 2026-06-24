namespace SecShare.Console.Services.Archive;

public interface IZipArchiveService
{
    Task<ZipArchiveBuildResult> CreateFromPathAsync(string path, CancellationToken cancellationToken);

    Task<ZipArchiveBuildResult> CreateFromTextAsync(string text, CancellationToken cancellationToken);

    Task<ZipArchiveExtractResult> ExtractAsync(
        byte[] archiveBytes,
        string destinationPath,
        CancellationToken cancellationToken);
}
