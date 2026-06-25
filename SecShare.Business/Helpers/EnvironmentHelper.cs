namespace SecShare.Business.Helpers;

public static class EnvironmentHelper
{
    public static string GetHostName()
    {
        return Environment.GetEnvironmentVariable("HOSTNAME") ?? string.Empty;
    }
}
