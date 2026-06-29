using System.Net.Http.Json;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Headers;
using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private const string EncryptedUploadFileName = "secret_file";

    public async Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        ArgumentNullException.ThrowIfNull(options);
        ValidateUploadOptions(options);

        using var fileContent = new ProgressByteArrayContent(encryptedPayload, progress);
        using var content = new MultipartFormDataContent
        {
            { fileContent, "file", EncryptedUploadFileName },
            { new StringContent(options.Expires), "Options.Expires" },
            { new StringContent(options.Downloads.ToString()), "Options.Downloads" },
            { new StringContent(options.ContentType.ToString()), "Options.ContentType" }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, SecShareConstants.ApiFilesPath)
        {
            Content = content
        };
        request.Headers.Add(SecShareClientHeaders.ClientType, SecShareClientHeaders.ClientTypeConsole);
        request.Headers.UserAgent.ParseAdd("SecShareConsole/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessResponseAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<UploadHttpResult>(cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new InvalidOperationException("Upload response does not contain a file token.");
        }

        return result;
    }
}
