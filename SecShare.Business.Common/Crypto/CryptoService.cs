using System.Security.Cryptography;

namespace SecShare.Business.Common.Crypto;

public class CryptoService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public string GenerateAesKey()
        => Base64UrlEncode(RandomNumberGenerator.GetBytes(KeySizeBytes));

    public (byte[] Payload, string Base64UrlKey) Encrypt(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var base64UrlKey = this.GenerateAesKey();
        var payload = this.Encrypt(data, base64UrlKey);

        return (payload, base64UrlKey);
    }

    public byte[] Encrypt(byte[] data, string base64UrlKey)
    {
        ArgumentNullException.ThrowIfNull(data);

        var key = DecodeAesKey(base64UrlKey);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, data, ciphertext, tag);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return payload;
    }

    public byte[] Decrypt(byte[] payload, string base64UrlKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64UrlKey);

        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new ArgumentException("Encrypted payload is too short.", nameof(payload));
        }

        var key = DecodeAesKey(base64UrlKey);

        var ciphertextLength = payload.Length - NonceSizeBytes - TagSizeBytes;
        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var tag = payload.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = payload.AsSpan(NonceSizeBytes + TagSizeBytes, ciphertextLength);
        var plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private static byte[] DecodeAesKey(string base64UrlKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64UrlKey);

        var key = Base64UrlDecode(base64UrlKey);
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException("AES-256-GCM key must be exactly 32 bytes.", nameof(base64UrlKey));
        }

        return key;
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');

        return Convert.FromBase64String(base64);
    }
}

