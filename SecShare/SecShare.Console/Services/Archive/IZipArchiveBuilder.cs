namespace SecShare.Console.Services.Archive;

public interface IZipArchiveBuilder
{
    Task<ZipArchiveBuildResult> CreateFromPathAsync(string path, CancellationToken cancellationToken);

    Task<ZipArchiveBuildResult> CreateFromTextAsync(string text, CancellationToken cancellationToken);
}
