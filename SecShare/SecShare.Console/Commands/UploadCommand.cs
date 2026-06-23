using System.ComponentModel;
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

        await AnsiConsole.Progress()
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
                await RunStepAsync(ctx, "Zipping directory...", cancellationToken);
                await RunStepAsync(ctx, "Encrypting with AES-256-GCM...", cancellationToken);
                await RunStepAsync(ctx, "Uploading to secshare.me...", cancellationToken);
            });

        var link = "https://secshare.me/f/mockToken#mockKey";
        var details = new Markup(
            $"""
            [green]Upload completed[/]

            Link: [link={link}]{link}[/]
            Expires: [yellow]{Markup.Escape(settings.Expires)}[/]
            Downloads: [yellow]{settings.Downloads}[/]
            Password: [yellow]{(settings.HasPassword ? "enabled" : "disabled")}[/]
            Mode: [yellow]{(settings.IsText ? "text" : "file")}[/]
            """);

        AnsiConsole.Write(new Panel(details)
            .Header("[bold green]SecShare[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));

        return 0;
    }

    private static async Task RunStepAsync(ProgressContext context, string description, CancellationToken cancellationToken)
    {
        var task = context.AddTask(description, autoStart: false, maxValue: 100);
        task.StartTask();

        while (!task.IsFinished)
        {
            await Task.Delay(80, cancellationToken);
            task.Increment(10);
        }
    }
}
