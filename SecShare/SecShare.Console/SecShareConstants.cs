namespace SecShare.Console;

public static class SecShareConstants
{
    public const string ServiceBaseUrl = "https://secshare.me";
    public const string ApiFilesPath = "/api/files";
    public const string ShareFilesPath = "/f";

    public static readonly Uri ServiceBaseUri = new(ServiceBaseUrl);
}
