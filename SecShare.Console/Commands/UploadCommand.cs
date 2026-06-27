using System.ComponentModel;
using System.Text.Json;
using SecShare.Api.Common.Dto.Storage;
using SecShare.Business.Exceptions;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Models.Http;
using SecShare.Console.Models.Upload;
using SecShare.Console.Services.Archive;
using SecShare.Console.Services.Http;
using SecShare.Console.Services.Upload;
using SecShare.Console.Ui;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SecShare.Console.Commands;

public sealed class UploadCommand : AsyncCommand<UploadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to a file or directory to upload.")]
        public string Path { get; init; } = string.Empty;

        [CommandOption("-e|--expires <expires>")]
        [DefaultValue("24h")]
        [Description("Expiration time, for example 24h, 7d, or 30m.")]
        public string Expires { get; init; } = "24h";

        [CommandOption("-d|--downloads <count>")]
        [DefaultValue(1)]
        [Description("Maximum number of downloads.")]
        public int Downloads { get; init; } = 1;

        [CommandOption("--text")]
        [Description("Treat input as plain text instead of a file path.")]
        public bool IsText { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold]SecShare[/] client-side encrypted upload");
        AnsiConsole.MarkupLine($"Target: [cyan]{Markup.Escape(settings.Path)}[/]");
        AnsiConsole.WriteLine();

        UploadPackage package;
        UploadHttpResult uploadResult;
        try
        {
            var archiveService = new ZipArchiveService();
            var uploadPackageService = new UploadPackageService(new CryptoService());
            using var httpClient = new HttpClient
            {
                BaseAddress = SecShareConstants.ServiceBaseUri,
                Timeout = TimeSpan.FromMinutes(5),
            };
            var secShareHttpClient = new SecShareHttpClient(httpClient);

            (package, uploadResult) = await AnsiConsole.Progress()
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
                    var zipTask = ctx.AddTask("Zipping directory...", autoStart: true, maxValue: 100);
                    var archive = settings.IsText
                        ? await archiveService.CreateFromTextAsync(settings.Path, cancellationToken)
                        : await archiveService.CreateFromPathAsync(settings.Path, cancellationToken);
                    zipTask.Value = 100;
                    zipTask.StopTask();

                    var encryptTask = ctx.AddTask("Encrypting with AES-256-GCM...", autoStart: true, maxValue: 100);
                    var createdPackage = uploadPackageService.EncryptArchive(archive);
                    encryptTask.Value = 100;
                    encryptTask.StopTask();

                    var uploadTask = ctx.AddTask(
                        $"Uploading to {SecShareConstants.ServiceBaseUri.Host}...",
                        autoStart: true,
                        maxValue: Math.Max(createdPackage.EncryptedPayload.LongLength, 1));
                    var result = await secShareHttpClient.UploadAsync(
                        createdPackage.EncryptedPayload,
                        new UploadFileOptions
                        {
                            Expires = settings.Expires,
                            Downloads = settings.Downloads
                        },
                        progress => TransferProgressUi.Update(
                            uploadTask,
                            $"Uploading to {SecShareConstants.ServiceBaseUri.Host}...",
                            progress),
                        cancellationToken);
                    Complete(uploadTask);

                    return (createdPackage, result);
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[red]Upload failed:[/] The request timed out.");
            return 1;
        }
        catch (Exception exception) when (exception is
            ArgumentException
            or HttpRequestException
            or IOException
            or JsonException
            or UnauthorizedAccessException
            or InvalidOperationException
            or ApiException)
        {
            AnsiConsole.MarkupLine($"[red]Upload failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }

        var links = UploadShareLinkBuilder.Create(uploadResult.Token, package.EncryptionKey);
        var details = CreateUploadSummary(settings, package, links);

        AnsiConsole.Write(new Panel(details)
            .Header("[bold green]SecShare[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

        return 0;
    }

    private static void Complete(ProgressTask task)
    {
        task.Value = task.MaxValue;
        task.StopTask();
    }

    private static Markup CreateUploadSummary(Settings settings, UploadPackage package, UploadShareLinks links)
        => new(
            $"""
            [green]Upload package prepared[/]

            [bold]Metadata[/]
            Source: [yellow]{Markup.Escape(package.SourceName)}[/]
            Mode: [yellow]{(settings.IsText ? "text" : "file")}[/]
            Files: [yellow]{package.FileCount}[/]
            Source size: [yellow]{TransferProgressUi.FormatBytes(package.SourceSizeBytes)}[/]
            Archive size: [yellow]{TransferProgressUi.FormatBytes(package.ArchiveSizeBytes)}[/]
            Encrypted size: [yellow]{TransferProgressUi.FormatBytes(package.EncryptedPayload.LongLength)}[/]

            [bold]Upload information[/]
            {Markup.Escape(UploadShareOutputFormatter.Format(links))}

            Download command: [yellow]secshare get "{Markup.Escape(links.FullSecureLink)}"[/]
            Expires: [yellow]{Markup.Escape(settings.Expires)}[/]
            Downloads before deletion: [yellow]{settings.Downloads}[/]
            """);

}
