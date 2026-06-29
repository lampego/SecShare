using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Http;
using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public interface ISecShareHttpClient
{
    Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );

    Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}
