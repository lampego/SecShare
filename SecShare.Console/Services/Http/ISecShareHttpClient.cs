using SecShare.Business.Common.Dto.Storage;
using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public interface ISecShareHttpClient
{
    Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken);

    Task<DownloadHttpResult> DownloadAsync(
        Uri payloadUri,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken);
}
