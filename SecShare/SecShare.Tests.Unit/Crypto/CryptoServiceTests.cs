using System.Security.Cryptography;
using SecShare.Business.Services.Crypto;

namespace SecShare.Tests.Unit.Crypto;

public sealed class CryptoServiceTests
{
    private readonly CryptoService cryptoService = new();

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalData()
    {
        var data = "client-side encryption payload"u8.ToArray();

        var (payload, key) = this.cryptoService.Encrypt(data);
        var decrypted = this.cryptoService.Decrypt(payload, key);

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void Encrypt_WithGeneratedAesKey_ThenDecrypt_ReturnsOriginalData()
    {
        var data = "payload encrypted with explicit generated key"u8.ToArray();
        var key = this.cryptoService.GenerateAesKey();

        var payload = this.cryptoService.Encrypt(data, key);
        var decrypted = this.cryptoService.Decrypt(payload, key);

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        var data = "secret payload"u8.ToArray();
        var (payload, _) = this.cryptoService.Encrypt(data);
        var (_, wrongKey) = this.cryptoService.Encrypt("another payload"u8.ToArray());

        Assert.ThrowsAny<CryptographicException>(() => this.cryptoService.Decrypt(payload, wrongKey));
    }

    [Fact]
    public void Decrypt_WithTamperedPayload_ThrowsCryptographicException()
    {
        var data = "authenticated payload"u8.ToArray();
        var (payload, key) = this.cryptoService.Encrypt(data);

        payload[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => this.cryptoService.Decrypt(payload, key));
    }
}
