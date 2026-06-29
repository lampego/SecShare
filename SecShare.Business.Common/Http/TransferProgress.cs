namespace SecShare.Business.Common.Http;

public sealed record TransferProgress(
    long BytesTransferred,
    long? TotalBytes,
    double BytesPerSecond
);

