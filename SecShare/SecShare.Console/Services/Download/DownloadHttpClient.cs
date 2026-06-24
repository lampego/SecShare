using SecShare.Console.Services.Archive;

namespace SecShare.Console.Services.Download;

public sealed class DownloadHttpClient(HttpClient httpClient) : IDownloadHttpClient
{
    public const long MaxEncryptedPayloadSizeBytes = ZipArchiveService.MaxSourceSizeBytes + (10L * 1024 * 1024);

    public async Task<byte[]> DownloadAsync(Uri payloadUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payloadUri);

        using var request = new HttpRequestMessage(HttpMethod.Get, payloadUri);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaxEncryptedPayloadSizeBytes)
        {
            throw new InvalidOperationException("Encrypted payload size must not exceed 210 MB.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = new MemoryStream();
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
        }

        return target.ToArray();
    }
}
