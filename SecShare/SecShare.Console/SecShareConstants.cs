namespace SecShare.Console;

public static class SecShareConstants
{
    public const string DefaultServiceBaseUrl = "https://secshare.me";
    public const string ApiFilesPath = "/api/files";
    public const string ShareFilesPath = "/f";

    public static string ServiceBaseUrl => 
        Environment.GetEnvironmentVariable("SECSHARE_API_URL") ?? DefaultServiceBaseUrl;

    public static Uri ServiceBaseUri => new(ServiceBaseUrl);
}
