using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Extensions.Autofac.DependencyInjection;
using SecShare.Business;
using SecShare.Business.Helpers;
using SecShare.Business.Logging;

namespace SecShare.WorkerServices;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var log = LoggerInitializer.BuildSerilogInstance();
        Log.Logger = log;

        Serilog.Debugging.SelfLog.Enable(Console.WriteLine);
        try
        {
            await CreateHostBuilder(args).RunAsync();
        }
        catch (Exception e)
        {
            log.Error(e, "Start application failed");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog(Log.Logger)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureAppConfiguration(config =>
            {
                config.ConfigureConfigurationProvider();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .Build();
    }
}
