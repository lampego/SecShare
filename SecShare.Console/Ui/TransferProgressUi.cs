using SecShare.Console.Models.Http;
using Spectre.Console;

namespace SecShare.Console.Ui;

public static class TransferProgressUi
{
    public static void Update(
        ProgressTask task,
        string operation,
        TransferProgress progress)
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
}
