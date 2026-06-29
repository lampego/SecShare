using System.Diagnostics;

namespace SecShare.Business.Common.Http;

public sealed class TransferProgressTracker(
    long? totalBytes,
    Action<TransferProgress>? progress)
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _bytesTransferred;
    private long _lastReportedBytes;
    private TimeSpan _lastReportedAt;
    private double _bytesPerSecond;

    public void Report(int bytesTransferred)
    {
        _bytesTransferred += bytesTransferred;

        var elapsed = _stopwatch.Elapsed;
        var interval = elapsed - _lastReportedAt;
        if (interval.TotalMilliseconds > 0)
        {
            var currentSpeed = (_bytesTransferred - _lastReportedBytes) / interval.TotalSeconds;
            _bytesPerSecond = _bytesPerSecond == 0
                ? currentSpeed
                : (_bytesPerSecond * 0.7) + (currentSpeed * 0.3);
        }

        _lastReportedBytes = _bytesTransferred;
        _lastReportedAt = elapsed;
        progress?.Invoke(new TransferProgress(_bytesTransferred, totalBytes, _bytesPerSecond));
    }

    public void Complete()
        => progress?.Invoke(new TransferProgress(
            _bytesTransferred,
            totalBytes ?? _bytesTransferred,
            _bytesPerSecond));
}

