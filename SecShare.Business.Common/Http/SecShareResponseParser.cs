using System.Globalization;
using System.Net.Http;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;

namespace SecShare.Business.Common.Http;

/// <summary>
/// Parses SecShare API response headers into a <see cref="DownloadResult"/>.
/// Used by both the Console and Blazor Web download clients.
/// </summary>
public static class SecShareResponseParser
{
    public static DownloadResult ParseDownloadResult(HttpResponseMessage response, byte[] payload)
    {
        return new DownloadResult(
            payload,
            ReadContentType(response),
            ReadHeader(response, SecShareFileHeaders.FileId),
            ReadHeader(response, SecShareFileHeaders.FileExtension),
            ReadLongHeader(response, SecShareFileHeaders.FileSize),
            ReadIntHeader(response, SecShareFileHeaders.DownloadsRemaining),
            ReadDateTimeHeader(response, SecShareFileHeaders.DeleteAt),
            ReadHeader(response, SecShareFileHeaders.PayloadType)
        );
    }

    public static string? ReadHeader(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values)
            ? values.FirstOrDefault()
            : null;

    private static StorageContentType ReadContentType(HttpResponseMessage response)
    {
        var value = ReadHeader(response, SecShareFileHeaders.ContentType);
        return Enum.TryParse<StorageContentType>(value, ignoreCase: true, out var contentType)
            ? contentType
            : StorageContentType.File;
    }

    private static long? ReadLongHeader(HttpResponseMessage response, string name)
        => long.TryParse(
            ReadHeader(response, name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;

    private static int? ReadIntHeader(HttpResponseMessage response, string name)
        => int.TryParse(
            ReadHeader(response, name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;

    private static DateTime? ReadDateTimeHeader(HttpResponseMessage response, string name)
        => DateTime.TryParse(
            ReadHeader(response, name),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var value
        )
            ? value
            : null;
}

