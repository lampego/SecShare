namespace SecShare.Console.Services.Upload.Http;

public sealed record UploadHttpOptions(
    string Expires,
    int Downloads,
    bool HasPassword,
    string SourceName);
