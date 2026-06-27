using System.Diagnostics;
using SecShare.Console.Models.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private sealed class TransferProgressTracker(
        long? totalBytes,
        Action<TransferProgress>? progress)
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private long bytesTransferred;
        private long lastReportedBytes;
        private TimeSpan lastReportedAt;
        private double bytesPerSecond;

        public void Report(int bytesTransferred)
        {
            this.bytesTransferred += bytesTransferred;

            var elapsed = this.stopwatch.Elapsed;
            var interval = elapsed - this.lastReportedAt;
            if (interval.TotalMilliseconds > 0)
            {
                var currentSpeed =
                    (this.bytesTransferred - this.lastReportedBytes) / interval.TotalSeconds;
                this.bytesPerSecond = this.bytesPerSecond == 0
                    ? currentSpeed
                    : (this.bytesPerSecond * 0.7) + (currentSpeed * 0.3);
            }

            this.lastReportedBytes = this.bytesTransferred;
            this.lastReportedAt = elapsed;
            progress?.Invoke(new TransferProgress(
                this.bytesTransferred,
                totalBytes,
                this.bytesPerSecond));
        }

        public void Complete()
            => progress?.Invoke(new TransferProgress(
                this.bytesTransferred,
                totalBytes ?? this.bytesTransferred,
                this.bytesPerSecond));
    }
}
