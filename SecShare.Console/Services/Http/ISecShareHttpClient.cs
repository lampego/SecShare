using SecShare.Business.Common.Http;
using SecShare.Business.Common.Http.Clients;

namespace SecShare.Console.Services.Http;

public interface ISecShareHttpClient
    : IUploadClient
{
    Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}
