using System.ComponentModel;
using System.Security.Cryptography;
using SecShare.Business.Common.Enums;
using SecShare.Business.Exceptions;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Models.Archive;
using SecShare.Console.Services.Archive;
using SecShare.Console.Services.Download;
using SecShare.Console.Services.Http;
using SecShare.Console.Ui;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SecShare.Console.Commands;

public sealed class DownloadCommand : AsyncCommand<DownloadCommand.Settings>
{
    private sealed record DownloadCommandResult(
        StorageContentType ContentType,
        ZipArchiveExtractResult? ExtractResult,
        string? Text
    );

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<url>")]
        [Description("SecShare file URL with #key, or URL without key to enter the key interactively.")]
        public string Url { get; init; } = string.Empty;

        [CommandArgument(1, "[path]")]
        [DefaultValue(".")]
        [Description("Directory where downloaded content will be extracted.")]
        public string Path { get; init; } = ".";
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        DownloadCommandResult result;
        try
        {
            var link = new SecShareDownloadLinkParser().Parse(settings.Url);

            AnsiConsole.MarkupLine("[bold]SecShare[/] secure download");
            AnsiConsole.MarkupLine($"Source: [cyan]{Markup.Escape(link.ShareUri.GetLeftPart(UriPartial.Path))}[/]");
            AnsiConsole.MarkupLine($"Destination: [cyan]{Markup.Escape(settings.Path)}[/]");
            AnsiConsole.WriteLine();

            var decryptionKey = new DecryptionKeyResolver(new ConsoleDecryptionKeyReader()).Resolve(link);

            using var httpClient = new HttpClient
            {
                BaseAddress = SecShareConstants.ServiceBaseUri,
                Timeout = TimeSpan.FromMinutes(5),
            };
            var secShareHttpClient = new SecShareHttpClient(httpClient);
            var packageService = new DownloadPackageService(new CryptoService());
            var archiveService = new ZipArchiveService();

            result = await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(TransferProgressUi.CreateColumns())
                .StartAsync(async ctx =>
                {
                    var downloadTask = ctx.AddTask("Downloading encrypted payload...", autoStart: true);
                    var downloadResult = await secShareHttpClient.DownloadAsync(
                        link.FileId,
                        progress => TransferProgressUi.Update(
                            downloadTask,
                            "Downloading encrypted payload...",
                            progress
                        ),
                        cancellationToken
                    );
                    Complete(downloadTask);

                    var decryptTask = ctx.AddTask("Decrypting data...", autoStart: true);
                    var archiveBytes = packageService.Decrypt(downloadResult.EncryptedPayload, decryptionKey);
                    Complete(decryptTask);

                    if (downloadResult.ContentType == StorageContentType.Text)
                    {
                        var textTask = ctx.AddTask("Reading text...", autoStart: true);
                        var text = await archiveService.ReadTextAsync(archiveBytes, cancellationToken);
                        Complete(textTask);

                        return new DownloadCommandResult(StorageContentType.Text, null, text);
                    }

                    var extractTask = ctx.AddTask("Extracting files...", autoStart: true);
                    var extractResult = await archiveService.ExtractAsync(
                        archiveBytes,
                        settings.Path,
                        cancellationToken
                    );
                    Complete(extractTask);

                    return new DownloadCommandResult(downloadResult.ContentType, extractResult, null);
                }
                );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[red]Download failed:[/] The request timed out.");
            return 1;
        }
        catch (Exception exception) when (
            exception is
                CryptographicException
                or FormatException
        )
        {
            AnsiConsole.MarkupLine("[red]Download failed:[/] Could not decrypt this file. Check the decryption key and try again.");
            return 1;
        }
        catch (Exception exception) when (
            exception is
                ArgumentException
                or HttpRequestException
                or IOException
                or UnauthorizedAccessException
                or InvalidOperationException
                or ApiException
        )
        {
            var errorMessage = ConsoleErrorParser.ResolveFriendlyDownloadErrorMessage(exception);
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {Markup.Escape(errorMessage)}");
            return 1;
        }

        if (result.ContentType == StorageContentType.Text)
        {
            var rawText = result.Text ?? string.Empty;
            var longestLineLength = rawText
                .Split('\n')
                .Select(line => line.TrimEnd('\r').Length)
                .DefaultIfEmpty(0)
                .Max();

            var panel = new Panel(Markup.Escape(rawText))
                .Header("[bold green]Decrypted text[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green);

            panel.Width = Math.Max(20, longestLineLength + 4);

            AnsiConsole.Write(panel);

            return 0;
        }

        var extractResult = result.ExtractResult
            ?? throw new InvalidOperationException("Download did not produce extracted files.");
        var extractedPaths = string.Join(
            Environment.NewLine,
            extractResult.ExtractedPaths.Select(path => $"[cyan]{Markup.Escape(path)}[/]")
        );
        var summary = new Markup(
            $"""
            [green]Downloaded content was decrypted and extracted.[/]

            Files: [yellow]{extractResult.FileCount}[/]
            Size: [yellow]{TransferProgressUi.FormatBytes(extractResult.ExtractedSizeBytes)}[/]
            Saved:
            {extractedPaths}
            """
        );

        AnsiConsole.Write(new Panel(summary)
            .Header("[bold green]Download completed[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
        );

        return 0;
    }

    private static void Complete(ProgressTask task)
    {
        task.Value = task.MaxValue;
        task.StopTask();
    }

}
