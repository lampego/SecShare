namespace SecShare.Business.Common.Dto.Storage;

public static class UploadFileExpiration
{
    public const string ValidationErrorMessage =
        "Expires must use a positive duration from 1 second to 365 days with suffix s, m, h, or d.";

    public static readonly TimeSpan MaxValue = TimeSpan.FromDays(365);

    public static bool TryParse(string? expires, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrWhiteSpace(expires) || expires.Length is < 2 or > 10)
        {
            return false;
        }

        var suffix = expires[^1];
        if (suffix is not ('s' or 'm' or 'h' or 'd'))
        {
            return false;
        }

        if (!int.TryParse(expires[..^1], out var value) || value <= 0)
        {
            return false;
        }

        duration = suffix switch
        {
            's' => TimeSpan.FromSeconds(value),
            'm' => TimeSpan.FromMinutes(value),
            'h' => TimeSpan.FromHours(value),
            'd' => TimeSpan.FromDays(value),
            _ => default
        };

        return duration <= MaxValue;
    }
}
