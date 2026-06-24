using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using SecShare.Console.Models.Http;
using SecShare.Console.Services.Archive;

namespace SecShare.Console.Services.Http;

public sealed class SecShareHttpClient(HttpClient httpClient) : ISecShareHttpClient
{
    public const long MaxEncryptedPayloadSizeBytes =
        ZipArchiveService.MaxSourceSizeBytes + (10L * 1024 * 1024);

    public async Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadHttpOptions options,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        ArgumentNullException.ThrowIfNull(options);

        using var fileContent = new ProgressByteArrayContent(encryptedPayload, progress);
        using var content = new MultipartFormDataContent
        {
            { fileContent, "file", $"{options.SourceName}.secshare" },
            { JsonContent.Create(options), "metadata" },
        };
        using var response = await httpClient.PostAsync(
            SecShareConstants.ApiFilesPath,
            content,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UploadHttpResult>(cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new InvalidOperationException("Upload response does not contain a file token.");
        }

        return result;
    }

    public async Task<byte[]> DownloadAsync(
        Uri payloadUri,
        Action<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payloadUri);

        using var request = new HttpRequestMessage(HttpMethod.Get, payloadUri);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

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

    private sealed class ProgressByteArrayContent(
        byte[] content,
        Action<TransferProgress>? progress)
        : HttpContent
    {
        private const int BufferSize = 81920;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => this.SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken)
        {
            var tracker = new TransferProgressTracker(content.LongLength, progress);

            for (var offset = 0; offset < content.Length; offset += BufferSize)
            {
                var count = Math.Min(BufferSize, content.Length - offset);
                await stream.WriteAsync(content.AsMemory(offset, count), cancellationToken);
                tracker.Report(count);
            }

            tracker.Complete();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.LongLength;
            return true;
        }
    }

    private sealed class TransferProgressTracker(
        long? totalBytes,
        Action<TransferProgress>? progress)
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private long bytesTransferred;
        private long lastReportedBytes;
        private TimeSpan lastReportedAt;
        private double bytesPerSecond;

        public void Report(int bytesTransferred)
        {
            this.bytesTransferred += bytesTransferred;

            var elapsed = this.stopwatch.Elapsed;
            var interval = elapsed - this.lastReportedAt;
            if (interval.TotalMilliseconds > 0)
            {
                var currentSpeed =
                    (this.bytesTransferred - this.lastReportedBytes) / interval.TotalSeconds;
                this.bytesPerSecond = this.bytesPerSecond == 0
                    ? currentSpeed
                    : (this.bytesPerSecond * 0.7) + (currentSpeed * 0.3);
            }

            this.lastReportedBytes = this.bytesTransferred;
            this.lastReportedAt = elapsed;
            progress?.Invoke(new TransferProgress(
                this.bytesTransferred,
                totalBytes,
                this.bytesPerSecond));
        }

        public void Complete()
            => progress?.Invoke(new TransferProgress(
                this.bytesTransferred,
                totalBytes ?? this.bytesTransferred,
                this.bytesPerSecond));
    }
}
