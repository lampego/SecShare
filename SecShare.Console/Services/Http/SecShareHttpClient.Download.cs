using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    public async Task<byte[]> DownloadAsync(
        Uri payloadUri,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payloadUri);

        using var request = new HttpRequestMessage(HttpMethod.Get, payloadUri);
        request.Headers.Add("X-Client-Type", "Console");
        request.Headers.UserAgent.ParseAdd("SecShareConsole/1.0");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
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
        return target.ToArray();
    }
}
