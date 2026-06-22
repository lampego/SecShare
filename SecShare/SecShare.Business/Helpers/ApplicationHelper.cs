using Microsoft.Extensions.Configuration;
using Serilog;

namespace SecShare.Business.Helpers;

public static class ApplicationHelper
{
    public static string HostingEnvironment { get; set; }
        = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    public static IConfigurationRoot BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .ConfigureConfigurationProvider()
            .Build();
    }

    public static IConfigurationBuilder ConfigureConfigurationProvider(this IConfigurationBuilder builder)
    {
        Log.Logger.Information("Initializing configuration with {HostingEnvironment} environment", HostingEnvironment);
        return builder
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
#if DEBUG
            .AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true)
#endif
            .AddJsonFile($"appsettings.{HostingEnvironment}.json", optional: true)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables();
    }
}
