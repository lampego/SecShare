using Microsoft.Extensions.Configuration;
using SecShare.Business.Helpers;
using SecShare.Business.Helpers.Tests;
using Serilog;
using Serilog.Core;

namespace SecShare.Business.Logging;

public static class LoggerInitializer
{
    public static LoggerConfiguration GetSerilogBuilder(bool isEnableInitLogging = true)
    {
        var environment = ApplicationHelper.HostingEnvironment;
        var configuration = ApplicationHelper.BuildConfiguration();
        var logBuilder = new LoggerConfiguration().ReadFrom.Configuration(configuration);

        if (UnitTestDetector.IsRunningFromXUnit)
        {
            return logBuilder;
        }

        if (isEnableInitLogging)
        {
            Log.Information("Init Serilog configuration for {HostingEnvironment} environment", environment);
        }

        logBuilder.Enrich.WithProperty("Environment", environment);
        logBuilder.Enrich.WithProperty("AppName", configuration.GetValue<string>("App:Name"));
        return logBuilder;
    }

    public static Logger BuildSerilogInstance()
    {
        return GetSerilogBuilder().CreateLogger();
    }
}
