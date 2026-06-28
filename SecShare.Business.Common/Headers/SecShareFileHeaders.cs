namespace SecShare.Business.Common.Headers;

public static class SecShareFileHeaders
{
    public const string ContentType = "X-SecShare-Content-Type";
    public const string FileId = "X-SecShare-File-Id";
    public const string FileExtension = "X-SecShare-File-Extension";
    public const string FileSize = "X-SecShare-File-Size";
    public const string DownloadsRemaining = "X-SecShare-Downloads-Remaining";
    public const string DeleteAt = "X-SecShare-Delete-At";
    public const string PayloadType = "X-SecShare-Payload-Type";

    public const string EncryptedArchivePayloadType = "encrypted-archive";
}
