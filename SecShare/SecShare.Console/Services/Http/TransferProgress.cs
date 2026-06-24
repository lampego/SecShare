namespace SecShare.Console.Services.Http;

public sealed record TransferProgress(
    long BytesTransferred,
    long? TotalBytes,
    double BytesPerSecond);
