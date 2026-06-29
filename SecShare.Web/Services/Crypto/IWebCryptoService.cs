namespace SecShare.Web.Services.Crypto;

/// <summary>
/// Provides browser-first AES-256-GCM encryption for SecShare payloads.
/// Keys are unpadded base64url-encoded 32-byte AES keys. Payloads are stored as
/// 12-byte nonce, 16-byte authentication tag, then ciphertext.
/// </summary>
public interface IWebCryptoService
{
    /// <summary>
    /// Generates a random 32-byte AES key and encrypts <paramref name="data"/>.
    /// The returned payload layout is nonce, tag, ciphertext.
    /// </summary>
    Task<WebCryptoEncryptionResult> EncryptAsync(byte[] data);

    /// <summary>
    /// Encrypts <paramref name="data"/> with an unpadded base64url-encoded 32-byte AES key.
    /// The returned payload layout is nonce, tag, ciphertext.
    /// </summary>
    Task<byte[]> EncryptAsync(byte[] data, string base64UrlKey);

    /// <summary>
    /// Decrypts a SecShare AES-GCM payload laid out as nonce, tag, ciphertext
    /// using an unpadded base64url-encoded 32-byte AES key.
    /// </summary>
    Task<byte[]> DecryptAsync(byte[] payload, string base64UrlKey);
}
