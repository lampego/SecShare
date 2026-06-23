using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
            .ConfigureWebHostDefaults(builder =>
            {
                builder
                    .UseStartup<TestStartup>()
                    .ConfigureTestServices(services => services.AddHttpContextAccessor())
                    .UseTestServer();
            });
    }
}
