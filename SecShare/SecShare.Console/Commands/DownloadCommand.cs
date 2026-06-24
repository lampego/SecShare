using System.ComponentModel;
using System.Security.Cryptography;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Services.Archive;
using SecShare.Console.Services.Download;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SecShare.Console.Commands;

public sealed class DownloadCommand : AsyncCommand<DownloadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<url>")]
        [Description("SecShare URL to download.")]
        public string Url { get; init; } = string.Empty;

        [CommandArgument(1, "[path]")]
        [DefaultValue(".")]
        [Description("Directory where downloaded content will be extracted.")]
        public string Path { get; init; } = ".";
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold]SecShare[/] secure download");
        AnsiConsole.MarkupLine($"Source: [cyan]{Markup.Escape(settings.Url)}[/]");
        AnsiConsole.MarkupLine($"Destination: [cyan]{Markup.Escape(settings.Path)}[/]");
        AnsiConsole.WriteLine();

        ZipArchiveExtractResult result;
        try
        {
            var link = new SecShareDownloadLinkParser().Parse(settings.Url);
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
            };
            var downloadHttpClient = new DownloadHttpClient(httpClient);
            var packageService = new DownloadPackageService(new CryptoService());
            var archiveService = new ZipArchiveService();

            result = await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new SpinnerColumn(),
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn())
                .StartAsync(async ctx =>
                {
                    var downloadTask = ctx.AddTask("Downloading encrypted payload...", autoStart: true);
                    var encryptedPayload = await downloadHttpClient.DownloadAsync(
                        link.PayloadUri,
                        cancellationToken);
                    Complete(downloadTask);

                    var decryptTask = ctx.AddTask("Decrypting data...", autoStart: true);
                    var archiveBytes = packageService.Decrypt(encryptedPayload, link.EncryptionKey);
                    Complete(decryptTask);

                    var extractTask = ctx.AddTask("Extracting files...", autoStart: true);
                    var extractResult = await archiveService.ExtractAsync(
                        archiveBytes,
                        settings.Path,
                        cancellationToken);
                    Complete(extractTask);

                    return extractResult;
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[red]Download failed:[/] The request timed out.");
            return 1;
        }
        catch (Exception exception) when (exception is
            ArgumentException
            or FormatException
            or CryptographicException
            or HttpRequestException
            or IOException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }

        var extractedPaths = string.Join(
            Environment.NewLine,
            result.ExtractedPaths.Select(path => $"[cyan]{Markup.Escape(path)}[/]"));
        var summary = new Markup(
            $"""
            [green]Downloaded content was decrypted and extracted.[/]

            Files: [yellow]{result.FileCount}[/]
            Size: [yellow]{FormatBytes(result.ExtractedSizeBytes)}[/]
            Saved:
            {extractedPaths}
            """);

        AnsiConsole.Write(new Panel(summary)
            .Header("[bold green]Download completed[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

        return 0;
    }

    private static void Complete(ProgressTask task)
    {
        task.Value = task.MaxValue;
        task.StopTask();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
