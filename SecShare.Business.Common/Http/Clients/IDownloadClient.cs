namespace SecShare.Business.Common.Http.Clients;

/// <summary>
/// Downloads an encrypted payload from the SecShare API.
/// Shared contract used by both the Blazor Web app and the Console.
/// </summary>
public interface IDownloadClient
{
    Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}