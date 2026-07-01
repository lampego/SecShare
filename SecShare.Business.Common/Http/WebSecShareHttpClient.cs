using System.Net.Http;
using System.Net.Http.Json;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Headers;

namespace SecShare.Business.Common.Http;

/// <summary>
/// Blazor WASM implementation of the SecShare API client.
/// Sends <c>X-Client-Type: Web</c> and streams encrypted payloads with optional progress reporting.
/// </summary>
public sealed class WebSecShareHttpClient(HttpClient httpClient) : ISecShareDownloadClient, ISecShareUploadClient
{
    private const long MaxEncryptedPayloadSizeBytes = 220L * 1024 * 1024;
    private const string ApiFilesPath = "/api/files";
    private const string EncryptedUploadFileName = "secret_file";

    public async Task<UploadResult> UploadAsync(
        byte[] encryptedPayload,
        UploadFileOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        SecShareUploadOptionsValidator.Validate(options);

        using var fileContent = new ProgressByteArrayContent(encryptedPayload, progress);
        using var content = new MultipartFormDataContent
        {
            { fileContent, "file", EncryptedUploadFileName },
            { new StringContent(options.Expires), "Options.Expires" },
            { new StringContent(options.Downloads.ToString()), "Options.Downloads" },
            { new StringContent(options.ContentType.ToString()), "Options.ContentType" }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiFilesPath)
        {
            Content = content
        };
        request.Headers.Add(SecShareClientHeaders.ClientType, SecShareClientHeaders.ClientTypeWeb);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await SecShareHttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<UploadResult>(cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new InvalidOperationException("Upload response does not contain a file token.");
        }

        return result;
    }

    public async Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var uri = new Uri($"{ApiFilesPath}/{Uri.EscapeDataString(fileId)}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add(SecShareClientHeaders.ClientType, SecShareClientHeaders.ClientTypeWeb);

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        await SecShareHttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

        var totalBytes = response.Content.Headers.ContentLength;
        if (totalBytes > MaxEncryptedPayloadSizeBytes)
        {
            throw new InvalidOperationException("Encrypted payload size must not exceed 220 MB.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new MemoryStream();
        var tracker = new TransferProgressTracker(totalBytes, progress);
        var buffer = new byte[81920];

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (target.Length + bytesRead > MaxEncryptedPayloadSizeBytes)
            {
                throw new InvalidOperationException("Encrypted payload size must not exceed 220 MB.");
            }

            await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            tracker.Report(bytesRead);
        }

        tracker.Complete();
        return SecShareResponseParser.ParseDownloadResult(response, target.ToArray());
    }
}
