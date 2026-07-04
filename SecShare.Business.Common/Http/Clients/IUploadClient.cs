using SecShare.Business.Common.Dto.Storage;

namespace SecShare.Business.Common.Http.Clients;

public interface IUploadClient
{
    Task<UploadResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    );
}