namespace SecShare.Console.Models.Upload;

public sealed record UploadShareLinks(
    string FullSecureLink,
    string Link,
    string DecryptionKey);
