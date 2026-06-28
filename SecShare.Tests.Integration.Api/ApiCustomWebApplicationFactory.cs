using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecShare.Business.Helpers;
using SecShare.Business.Logging;
using Serilog;

namespace SecShare.Tests.Integration.Api;

public class ApiCustomWebApplicationFactory : WebApplicationFactory<TestStartup>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        Log.Logger = LoggerInitializer.GetSerilogBuilder(isEnableInitLogging: false).CreateLogger();

        return Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .UseSerilog(Log.Logger)
            .ConfigureAppConfiguration(builder => builder.ConfigureConfigurationProvider())
            // Disable file-change tracking on all config sources.
            // Host.CreateDefaultBuilder() registers appsettings.json with reloadOnChange: true by default,
            // creating a FileSystemWatcher (inotify instance) per factory. Without this, each test class
            // exhausts the Linux inotify limit (128) when many test classes are run together.
            .ConfigureAppConfiguration((_, config) =>
            {
                foreach (var source in config.Sources.OfType<FileConfigurationSource>())
                {
                    source.ReloadOnChange = false;
                }
            })
            .ConfigureWebHostDefaults(builder =>
            {
                builder
                    .UseStartup<TestStartup>()
                    .ConfigureTestServices(services => services.AddHttpContextAccessor())
                    .UseTestServer();
            });
    }
}
