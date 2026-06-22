namespace SecShare.Business.Services.Storage.Client;

public interface IFileStorageClient
{
    Task<UploadedFileDto?> Upload(
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken = default
    );

    Task<Stream> GetAsStream(string filePath, CancellationToken cancellationToken = default);

    Task Delete(string filePath, CancellationToken cancellationToken = default);
}
