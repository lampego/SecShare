using System.Globalization;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;
using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    public async Task<DownloadHttpResult> DownloadAsync(
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
        return new DownloadHttpResult(
            target.ToArray(),
            ReadContentType(response),
            ReadHeader(response, SecShareFileHeaders.FileId),
            ReadHeader(response, SecShareFileHeaders.FileExtension),
            ReadLongHeader(response, SecShareFileHeaders.FileSize),
            ReadIntHeader(response, SecShareFileHeaders.DownloadsRemaining),
            ReadDateTimeHeader(response, SecShareFileHeaders.DeleteAt),
            ReadHeader(response, SecShareFileHeaders.PayloadType)
        );
    }

    private static StorageContentType ReadContentType(HttpResponseMessage response)
    {
        var value = ReadHeader(response, SecShareFileHeaders.ContentType);
        return Enum.TryParse<StorageContentType>(value, ignoreCase: true, out var contentType)
            ? contentType
            : StorageContentType.File;
    }

    private static string? ReadHeader(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values)
            ? values.SingleOrDefault()
            : null;
    }

    private static long? ReadLongHeader(HttpResponseMessage response, string name)
    {
        return long.TryParse(
            ReadHeader(response, name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;
    }

    private static int? ReadIntHeader(HttpResponseMessage response, string name)
    {
        return int.TryParse(
            ReadHeader(response, name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;
    }

    private static DateTime? ReadDateTimeHeader(HttpResponseMessage response, string name)
    {
        return DateTime.TryParse(
            ReadHeader(response, name),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var value
        )
            ? value
            : null;
    }
}
