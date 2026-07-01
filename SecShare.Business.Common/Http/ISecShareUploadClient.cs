using SecShare.Business.Common.Dto.Storage;

namespace SecShare.Business.Common.Http;

public interface ISecShareUploadClient
{
    Task<UploadResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}
