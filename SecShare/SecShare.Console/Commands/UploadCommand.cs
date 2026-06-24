using System.ComponentModel;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Services.Archive;
using SecShare.Console.Services.Upload;
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

        [CommandOption("-p|--password")]
        [Description("Require an additional password before decryption.")]
        public bool HasPassword { get; init; }

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
        try
        {
            var archiveService = new ZipArchiveService();
            var uploadPackageService = new UploadPackageService(new CryptoService());

            package = await AnsiConsole.Progress()
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

                    await RunMockStepAsync(ctx, "Uploading to secshare.me...", cancellationToken);

                    return createdPackage;
                });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            AnsiConsole.MarkupLine($"[red]Upload failed:[/] {Markup.Escape(exception.Message)}");
            return 1;
        }

        var token = $"mock-{Guid.NewGuid():N}"[..21];
        var link = $"https://secshare.me/f/{token}#{package.EncryptionKey}";
        var details = CreateUploadSummary(settings, package, link);

        AnsiConsole.Write(new Panel(details)
            .Header("[bold green]SecShare[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

        return 0;
    }

    private static async Task RunMockStepAsync(
        ProgressContext context,
        string description,
        CancellationToken cancellationToken)
    {
        var task = context.AddTask(description, autoStart: false, maxValue: 100);
        task.StartTask();

        while (!task.IsFinished)
        {
            await Task.Delay(80, cancellationToken);
            task.Increment(10);
        }
    }

    private static Markup CreateUploadSummary(Settings settings, UploadPackage package, string link)
        => new(
            $"""
            [green]Upload package prepared[/]

            Link: [link={link}]{link}[/]
            Source: [yellow]{Markup.Escape(package.SourceName)}[/]
            Files: [yellow]{package.FileCount}[/]
            Source size: [yellow]{FormatBytes(package.SourceSizeBytes)}[/]
            Archive size: [yellow]{FormatBytes(package.ArchiveSizeBytes)}[/]
            Encrypted size: [yellow]{FormatBytes(package.EncryptedPayload.LongLength)}[/]
            Expires: [yellow]{Markup.Escape(settings.Expires)}[/]
            Downloads: [yellow]{settings.Downloads}[/]
            Password: [yellow]{(settings.HasPassword ? "enabled" : "disabled")}[/]
            Mode: [yellow]{(settings.IsText ? "text" : "file")}[/]
            """);

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
