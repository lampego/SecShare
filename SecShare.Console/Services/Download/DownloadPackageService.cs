using SecShare.Business.Services.Crypto;

namespace SecShare.Console.Services.Download;

public sealed class DownloadPackageService(CryptoService cryptoService) : IDownloadPackageService
{
    public byte[] Decrypt(byte[] encryptedPayload, string encryptionKey)
        => cryptoService.Decrypt(encryptedPayload, encryptionKey);
}
