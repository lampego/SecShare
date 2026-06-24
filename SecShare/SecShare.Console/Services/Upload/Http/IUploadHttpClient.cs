namespace SecShare.Console.Services.Upload.Http;

public interface IUploadHttpClient
{
    Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadHttpOptions options,
        CancellationToken cancellationToken);
}
