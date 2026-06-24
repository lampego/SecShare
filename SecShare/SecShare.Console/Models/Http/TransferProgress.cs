namespace SecShare.Console.Models.Http;

public sealed record TransferProgress(
    long BytesTransferred,
    long? TotalBytes,
    double BytesPerSecond);
