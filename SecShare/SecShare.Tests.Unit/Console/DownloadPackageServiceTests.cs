using SecShare.Business.Services.Crypto;
using SecShare.Console.Services.Download;

namespace SecShare.Tests.Unit.Console;

public sealed class DownloadPackageServiceTests
{
    [Fact]
    public void Decrypt_WithEncryptedPayload_ReturnsArchiveBytes()
    {
        var cryptoService = new CryptoService();
        var service = new DownloadPackageService(cryptoService);
        var archiveBytes = "ZIP archive bytes"u8.ToArray();
        var (encryptedPayload, encryptionKey) = cryptoService.Encrypt(archiveBytes);

        var decrypted = service.Decrypt(encryptedPayload, encryptionKey);

        Assert.Equal(archiveBytes, decrypted);
    }
}
