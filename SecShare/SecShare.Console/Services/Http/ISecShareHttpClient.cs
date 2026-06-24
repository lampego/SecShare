namespace SecShare.Console.Services.Http;

public interface ISecShareHttpClient
{
    Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadHttpOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken);

    Task<byte[]> DownloadAsync(
        Uri payloadUri,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken);
}
