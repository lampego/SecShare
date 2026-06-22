using Autofac.Extensions.DependencyInjection;
using SecShare.Api;
using SecShare.Business.Helpers;
using SecShare.Business.Logging;
using Serilog;

using var log = LoggerInitializer.BuildSerilogInstance();
Log.Logger = log;

try
{
    CreateHostBuilder(args).Build().Run();
}
catch (Exception exception)
{
    log.Error(exception, "Start application failed");
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .UseSerilog(Log.Logger)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureAppConfiguration(config => config.ConfigureConfigurationProvider())
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}

public partial class Program
{
}
