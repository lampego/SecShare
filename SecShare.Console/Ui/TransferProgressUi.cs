using SecShare.Business.Common.Formatting;
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
        => ByteSizeFormatter.Format(bytes);

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
