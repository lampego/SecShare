namespace SecShare.Console.Services.Download;

public interface IDownloadHttpClient
{
    Task<byte[]> DownloadAsync(Uri payloadUri, CancellationToken cancellationToken);
}
