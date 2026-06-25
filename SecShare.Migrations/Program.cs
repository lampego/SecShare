using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SecShare.Migrations;

public class Program
{
    private static string? CustomConnectionString => Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTION_STRING");

    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var defaultConnectionString = string.IsNullOrEmpty(CustomConnectionString)
            ? configuration.GetConnectionString("DefaultConnection")
            : CustomConnectionString;

        if (string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured");
        }

        Migrate(defaultConnectionString);

        var testConnectionString = configuration.GetConnectionString("TestConnection");
        if (!string.IsNullOrWhiteSpace(testConnectionString))
        {
            Migrate(testConnectionString);
        }

        Console.WriteLine("Migrations are applied...");
    }

    public static void Migrate(string connectionString)
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(Assembly.GetExecutingAssembly()).For.All())
            .AddLogging(loggingBuilder => loggingBuilder.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}
