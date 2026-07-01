namespace SecShare.Business.Common.Formatting;

public static class ByteSizeFormatter
{
    public static string Format(double bytes)
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
}
