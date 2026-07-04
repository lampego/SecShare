using System.ComponentModel;
using System.Security.Cryptography;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Models.Archive;
using SecShare.Business.Common.Services.Archive;
using SecShare.Business.Exceptions;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Services.Download;
using SecShare.Console.Services.Http;
using SecShare.Console.Ui;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SecShare.Console.Commands;

public sealed class DownloadCommand : AsyncCommand<DownloadCommand.Settings>
{
    private const string OverwriteExistingChoice = "Replace existing files";
    private const string UseAnotherDirectoryChoice = "Extract to another directory";
    private const string CancelChoice = "Cancel download";

    private sealed record DownloadCommandResult(
        StorageContentType ContentType,
        byte[] ArchiveBytes
    );

    private sealed record ExtractionPlan(
        string DestinationPath,
        bool IsOverwriteEnabled
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
        var archiveService = new ZipArchiveService();
        DownloadCommandResult result;
        try
        {
            var link = new SecShareDownloadLinkParser().Parse(settings.Url);

            AnsiConsole.MarkupLine("[bold]SecShare[/] secure download");
            AnsiConsole.MarkupLine($"Source: [cyan]{Markup.Escape(link.ShareUri.GetLeftPart(UriPartial.Path))}[/]");
            AnsiConsole.MarkupLine($"Destination: [cyan]{Markup.Escape(settings.Path)}[/]");
            AnsiConsole.WriteLine();

            var decryptionKey = new DecryptionKeyResolver(new ConsoleDecryptionKeyReader()).Resolve(link);

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = SecShareConstants.ServiceBaseUri;
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            var secShareHttpClient = new SecShareHttpClient(httpClient);
            var packageService = new DownloadPackageService(new CryptoService());

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

                    return new DownloadCommandResult(downloadResult.ContentType, archiveBytes);
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

        try
        {
            if (result.ContentType == StorageContentType.Text)
            {
                var rawText = await archiveService.ReadTextAsync(result.ArchiveBytes, cancellationToken);
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

            var extractionPlan = ResolveExtractionPlan(settings.Path, result.ArchiveBytes, archiveService);
            if (extractionPlan is null)
            {
                AnsiConsole.MarkupLine("[yellow]Download cancelled before extraction.[/]");
                return 1;
            }

            var extractResult = await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(TransferProgressUi.CreateColumns())
                .StartAsync(async ctx =>
                {
                    var extractTask = ctx.AddTask("Extracting files...", autoStart: true);
                    var extracted = await archiveService.ExtractAsync(
                        result.ArchiveBytes,
                        extractionPlan.DestinationPath,
                        cancellationToken,
                        new ZipArchiveExtractOptions(extractionPlan.IsOverwriteEnabled)
                    );
                    Complete(extractTask);
                    return extracted;
                });

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
        catch (Exception exception) when (
            exception is
                InvalidDataException
                or ArgumentException
                or IOException
                or UnauthorizedAccessException
                or InvalidOperationException
        )
        {
            var errorMessage = ConsoleErrorParser.ResolveFriendlyDownloadErrorMessage(exception);
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {Markup.Escape(errorMessage)}");
            return 1;
        }
    }

    private static void Complete(ProgressTask task)
    {
        task.Value = task.MaxValue;
        task.StopTask();
    }

    private static ExtractionPlan? ResolveExtractionPlan(
        string destinationPath,
        byte[] archiveBytes,
        IZipArchiveService archiveService
    )
    {
        var currentDestinationPath = Path.GetFullPath(destinationPath);
        while (true)
        {
            EnsureDestinationIsDirectoryPath(currentDestinationPath);

            var conflictingPaths = archiveService.GetConflictingPaths(archiveBytes, currentDestinationPath);
            if (conflictingPaths.Count == 0)
            {
                return new ExtractionPlan(currentDestinationPath, IsOverwriteEnabled: false);
            }

            ShowConflictingPaths(currentDestinationPath, conflictingPaths);
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]How should SecShare handle the existing files?[/]")
                    .AddChoices(
                        OverwriteExistingChoice,
                        UseAnotherDirectoryChoice,
                        CancelChoice
                    )
            );

            switch (choice)
            {
                case OverwriteExistingChoice:
                    return new ExtractionPlan(currentDestinationPath, IsOverwriteEnabled: true);
                case UseAnotherDirectoryChoice:
                    currentDestinationPath = PromptForAlternativeDestinationPath(destinationPath);
                    break;
                case CancelChoice:
                    return null;
            }
        }
    }

    private static void EnsureDestinationIsDirectoryPath(string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            throw new IOException($"Destination '{destinationPath}' is an existing file. Please choose a directory path.");
        }
    }

    private static void ShowConflictingPaths(string destinationPath, IReadOnlyCollection<string> conflictingPaths)
    {
        AnsiConsole.MarkupLine(
            $"[yellow]The destination already contains files or directories from this archive:[/] [cyan]{Markup.Escape(destinationPath)}[/]"
        );

        foreach (var conflictingPath in conflictingPaths)
        {
            AnsiConsole.MarkupLine($"  • [yellow]{Markup.Escape(conflictingPath)}[/]");
        }

        AnsiConsole.WriteLine();
    }

    private static string PromptForAlternativeDestinationPath(string baseDestinationPath)
    {
        while (true)
        {
            var directoryName = AnsiConsole.Ask<string>(
                "[bold]Enter a subdirectory name inside the selected destination:[/]"
            ).Trim();

            if (!TryValidateDirectoryName(directoryName, out var validationErrorMessage))
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(validationErrorMessage)}[/]");
                continue;
            }

            return Path.Combine(Path.GetFullPath(baseDestinationPath), directoryName);
        }
    }

    private static bool TryValidateDirectoryName(string directoryName, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            validationErrorMessage = "Directory name must not be empty.";
            return false;
        }

        if (directoryName is "." or "..")
        {
            validationErrorMessage = "Directory name must not be '.' or '..'.";
            return false;
        }

        if (directoryName != Path.GetFileName(directoryName)
            || directoryName.Contains(Path.DirectorySeparatorChar)
            || directoryName.Contains(Path.AltDirectorySeparatorChar))
        {
            validationErrorMessage = "Directory name must not contain path separators.";
            return false;
        }

        if (directoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            validationErrorMessage = "Directory name contains invalid characters.";
            return false;
        }

        validationErrorMessage = string.Empty;
        return true;
    }

}
