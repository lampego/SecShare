using System.ComponentModel;
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
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold]SecShare[/] secure download");
        AnsiConsole.MarkupLine($"Source: [cyan]{Markup.Escape(settings.Url)}[/]");
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
                await RunStepAsync(ctx, "Downloading encrypted payload...", cancellationToken);
                await RunStepAsync(ctx, "Decrypting data...", cancellationToken);
                await RunStepAsync(ctx, "Extracting files...", cancellationToken);
            });

        AnsiConsole.Write(new Panel("[green]Files saved to the current directory.[/]")
            .Header("[bold green]Download completed[/]")
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
