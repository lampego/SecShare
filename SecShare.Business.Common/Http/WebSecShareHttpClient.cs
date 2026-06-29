using System.Net.Http;
using SecShare.Business.Common.Headers;

namespace SecShare.Business.Common.Http;

/// <summary>
/// Blazor WASM implementation of <see cref="ISecShareDownloadClient"/>.
/// Sends <c>X-Client-Type: Web</c> and streams the encrypted payload with optional progress reporting.
/// </summary>
public sealed class WebSecShareHttpClient(HttpClient httpClient) : ISecShareDownloadClient
{
    private const long MaxEncryptedPayloadSizeBytes = 220L * 1024 * 1024;
    private const string ApiFilesPath = "/api/files";

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
