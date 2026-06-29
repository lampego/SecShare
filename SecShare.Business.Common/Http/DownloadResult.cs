using SecShare.Business.Common.Enums;

namespace SecShare.Business.Common.Http;

public sealed record DownloadResult(
    byte[] EncryptedPayload,
    StorageContentType ContentType,
    string? FileId,
    string? Extension,
    long? Size,
    int? DownloadsRemaining,
    DateTime? DeleteAt,
    string? PayloadType
);

