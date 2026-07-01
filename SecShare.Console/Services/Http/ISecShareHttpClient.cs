using SecShare.Business.Common.Http;

namespace SecShare.Console.Services.Http;

public interface ISecShareHttpClient
    : ISecShareUploadClient
{
    Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}
