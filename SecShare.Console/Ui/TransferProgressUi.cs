using SecShare.Console.Models.Http;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SecShare.Console.Ui;

public static class TransferProgressUi
{
    public static ProgressColumn[] CreateColumns()
        => [new MultilineTransferProgressColumn()];

    public static void Update(
        ProgressTask task,
        string operation,
        TransferProgress progress
    )
    {
        var totalBytes = progress.TotalBytes;
        var maxValue = Math.Max(totalBytes ?? progress.BytesTransferred, 1);

        task.MaxValue = maxValue;
        task.Value = Math.Min(progress.BytesTransferred, maxValue);
        task.Description =
            $"{operation} [grey]{FormatBytes(progress.BytesTransferred)} / "
            + $"{FormatTotalBytes(totalBytes)} | {FormatBytes(progress.BytesPerSecond)}/s[/]";
    }

    public static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private static string FormatTotalBytes(long? totalBytes)
        => totalBytes.HasValue
            ? FormatBytes(totalBytes.Value)
            : "?";

    private sealed class MultilineTransferProgressColumn : ProgressColumn
    {
        private readonly ProgressBarColumn progressBarColumn = new();
        private readonly PercentageColumn percentageColumn = new();
        private readonly ElapsedTimeColumn elapsedTimeColumn = new();

        protected override bool NoWrap => true;

        public override IRenderable Render(
            RenderOptions options,
            ProgressTask task,
            TimeSpan deltaTime
        )
        {
            return new Rows(
                new Markup(task.Description),
                new Columns(
                    progressBarColumn.Render(options, task, deltaTime),
                    percentageColumn.Render(options, task, deltaTime),
                    elapsedTimeColumn.Render(options, task, deltaTime)
                )
            );
        }

        public override int? GetColumnWidth(RenderOptions options)
            => null;
    }
}
