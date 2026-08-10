using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Models.Archive;

namespace SecShare.Business.Common.Services.Archive;

public interface IZipArchiveService
{
    Task<ZipArchiveBuildResult> CreateFromPathAsync(string path, CancellationToken cancellationToken);

    Task<ZipArchiveBuildResult> CreateFromPathsAsync(
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken
    );

    Task<ZipArchiveBuildResult> CreateFromStreamsAsync(
        IReadOnlyCollection<ZipArchiveSourceItem> items,
        string sourceName,
        CancellationToken cancellationToken
    );

    Task<ZipArchiveBuildResult> CreateFromTextAsync(string text, CancellationToken cancellationToken);

    IReadOnlyCollection<string> GetConflictingPaths(byte[] archiveBytes, string destinationPath);

    Task<ZipArchiveExtractResult> ExtractAsync(
        byte[] archiveBytes,
        string destinationPath,
        CancellationToken cancellationToken,
        ZipArchiveExtractOptions? options = null
    );

    Task<string> ReadTextAsync(byte[] archiveBytes, CancellationToken cancellationToken);

    Task<ZipArchiveContentResult> ReadContentAsync(
        byte[] archiveBytes,
        StorageContentType contentType,
        CancellationToken cancellationToken
    );
}
