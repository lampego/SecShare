namespace SecShare.Console.Models.Http;

public sealed record UploadHttpOptions(
    string Expires,
    int Downloads,
    bool HasPassword,
    string SourceName);
