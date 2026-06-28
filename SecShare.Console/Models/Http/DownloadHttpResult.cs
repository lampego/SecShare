using SecShare.Business.Common.Enums;

namespace SecShare.Console.Models.Http;

public sealed record DownloadHttpResult(
    byte[] EncryptedPayload,
    StorageContentType ContentType,
    string? FileId,
    string? Extension,
    long? Size,
    int? DownloadsRemaining,
    DateTime? DeleteAt,
    string? PayloadType
);
