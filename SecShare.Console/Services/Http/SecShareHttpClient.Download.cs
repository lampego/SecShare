using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Common.Http.Clients;
using SecShare.Business.Common.Http.Parsers;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private const string ApiFilesPath = "/api/files";

    // Explicit IDownloadClient implementation — no progress reporting wrapper needed.
    Task<DownloadResult> IDownloadClient.DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
        => DownloadAsync(fileId, progress, cancellationToken);

    public async Task<DownloadResult> DownloadAsync(
        string fileId,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var uri = new Uri($"{ApiFilesPath}/{Uri.EscapeDataString(fileId)}", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add(SecShareClientHeaders.ClientType, SecShareClientHeaders.ClientTypeConsole);
        request.Headers.UserAgent.ParseAdd("SecShareConsole/1.0");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        await EnsureSuccessResponseAsync(response, cancellationToken);

        var totalBytes = response.Content.Headers.ContentLength;
        if (totalBytes > MaxEncryptedPayloadSizeBytes)
        {
            throw new InvalidOperationException("Encrypted payload size must not exceed 210 MB.");
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
                throw new InvalidOperationException("Encrypted payload size must not exceed 210 MB.");
            }

            await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            tracker.Report(bytesRead);
        }

        tracker.Complete();
        return ResponseParser.ParseDownloadResult(response, target.ToArray());
    }
}
