namespace SecShare.Web.Services.Crypto;

public sealed record WebCryptoEncryptionResult(
    byte[] Payload,
    string Base64UrlKey
);
