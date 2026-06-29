using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecShare.Business.Common.Crypto;

namespace SecShare.Web.Services.Crypto;

public sealed class WebCryptoService(
    IJSRuntime js,
    ILogger<WebCryptoService> logger
) : IWebCryptoService
{
    private static readonly CryptoService DotNetCryptoService = new();

    private const string BrowserCryptoErrorPrefix = "SecShareCrypto.";
    private const string BrowserCryptoUnsupportedError = BrowserCryptoErrorPrefix + "Unsupported";
    private const string BrowserCryptoInvalidKeyError = BrowserCryptoErrorPrefix + "InvalidKey";
    private const string BrowserCryptoInvalidPayloadError = BrowserCryptoErrorPrefix + "InvalidPayload";
    private const string BrowserCryptoDecryptionFailedError = BrowserCryptoErrorPrefix + "DecryptionFailed";

    public async Task<WebCryptoEncryptionResult> EncryptAsync(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var base64UrlKey = await GenerateAesKeyAsync();
        var payload = await EncryptAsync(data, base64UrlKey);

        return new WebCryptoEncryptionResult(payload, base64UrlKey);
    }

    public async Task<byte[]> EncryptAsync(byte[] data, string base64UrlKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64UrlKey);

        try
        {
            return await js.InvokeAsync<byte[]>("secshareInterop.encryptAesGcm", data, base64UrlKey);
        }
        catch (JSException ex) when (
            IsBrowserCryptoInvalidKeyException(ex)
        )
        {
            throw new ArgumentException("AES-256-GCM key must be exactly 32 bytes.", nameof(base64UrlKey), ex);
        }
        catch (JSException ex) when (
            IsBrowserCryptoUnsupportedException(ex)
        )
        {
            logger.LogInformation(ex, "Browser Web Crypto AES-GCM encryption is unavailable. Falling back to .NET AES-GCM.");
            return EncryptWithDotNet(data, base64UrlKey);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Browser Web Crypto AES-GCM encryption failed unexpectedly. Falling back to .NET AES-GCM.");
            return EncryptWithDotNet(data, base64UrlKey);
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] payload, string base64UrlKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64UrlKey);

        try
        {
            return await js.InvokeAsync<byte[]>("secshareInterop.decryptAesGcm", payload, base64UrlKey);
        }
        catch (JSException ex) when (
            IsBrowserCryptoInvalidDecryptionException(ex)
        )
        {
            throw new CryptographicException("Browser AES-GCM decryption failed.", ex);
        }
        catch (JSException ex) when (
            IsBrowserCryptoUnsupportedException(ex)
        )
        {
            logger.LogInformation(ex, "Browser Web Crypto AES-GCM decryption is unavailable. Falling back to .NET AES-GCM.");
            return DecryptWithDotNet(payload, base64UrlKey);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Browser Web Crypto AES-GCM decryption failed unexpectedly. Falling back to .NET AES-GCM.");
            return DecryptWithDotNet(payload, base64UrlKey);
        }
    }

    private async Task<string> GenerateAesKeyAsync()
    {
        try
        {
            return await js.InvokeAsync<string>("secshareInterop.generateAesKey");
        }
        catch (JSException ex) when (
            IsBrowserCryptoUnsupportedException(ex)
        )
        {
            logger.LogInformation(ex, "Browser Web Crypto key generation is unavailable. Falling back to .NET key generation.");
            return GenerateAesKeyWithDotNet();
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Browser Web Crypto key generation failed unexpectedly. Falling back to .NET key generation.");
            return GenerateAesKeyWithDotNet();
        }
    }

    private static string GenerateAesKeyWithDotNet()
        => DotNetCryptoService.GenerateAesKey();

    private static byte[] EncryptWithDotNet(byte[] data, string base64UrlKey)
        => DotNetCryptoService.Encrypt(data, base64UrlKey);

    private static byte[] DecryptWithDotNet(byte[] payload, string base64UrlKey)
        => DotNetCryptoService.Decrypt(payload, base64UrlKey);

    private static bool IsBrowserCryptoInvalidDecryptionException(JSException exception)
        => HasBrowserCryptoError(exception, BrowserCryptoInvalidKeyError)
           || HasBrowserCryptoError(exception, BrowserCryptoInvalidPayloadError)
           || HasBrowserCryptoError(exception, BrowserCryptoDecryptionFailedError);

    private static bool IsBrowserCryptoInvalidKeyException(JSException exception)
        => HasBrowserCryptoError(exception, BrowserCryptoInvalidKeyError);

    private static bool IsBrowserCryptoUnsupportedException(JSException exception)
        => HasBrowserCryptoError(exception, BrowserCryptoUnsupportedError);

    private static bool HasBrowserCryptoError(JSException exception, string errorCode)
        => exception.Message.Contains(errorCode, StringComparison.Ordinal);
}
