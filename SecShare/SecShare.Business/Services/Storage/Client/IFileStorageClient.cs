namespace SecShare.Business.Services.Storage.Client;

public interface IFileStorageClient
{
    Task SaveAsync(string path, Stream stream, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
