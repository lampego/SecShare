using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace SecShare.Api;

public class UploadRateLimitMiddleware
{
    public sealed class UploadRateLimitOptions
    {
        public const string SectionName = "UploadRateLimit";

        public int UploadBytesPerSecond { get; set; }
    }

    private static readonly PathString[] UploadPaths =
    [
        "/api/file/upload",
        "/api/files"
    ];

    private readonly RequestDelegate _next;
    private readonly int _uploadBytesPerSecond;

    public UploadRateLimitMiddleware(RequestDelegate next, IOptions<UploadRateLimitOptions> options)
    {
        _next = next;
        _uploadBytesPerSecond = options.Value.UploadBytesPerSecond;
    }

    public async Task Invoke(HttpContext context)
    {
        if (ShouldThrottleUpload(context.Request))
        {
            DisableKestrelRequestBodyLimit(context);

            var originalBody = context.Request.Body;
            var limitedBody = new RateLimitedReadStream(
                originalBody,
                _uploadBytesPerSecond
            );
            context.Request.Body = limitedBody;

            try
            {
                await _next(context);
            }
            finally
            {
                context.Request.Body = originalBody;
                await limitedBody.DisposeAsync();
            }

            return;
        }

        await _next(context);
    }

    private static void DisableKestrelRequestBodyLimit(HttpContext context)
    {
        var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature is { IsReadOnly: false })
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = null;
        }
    }

    private static bool ShouldThrottleUpload(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        foreach (var uploadPath in UploadPaths)
        {
            if (request.Path.Equals(uploadPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class RateLimitedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _bytesPerSecond;
        private readonly Stopwatch _stopwatch;
        private long _totalBytesRead;

        public RateLimitedReadStream(Stream inner, int bytesPerSecond)
        {
            _inner = inner;
            _bytesPerSecond = bytesPerSecond;
            _stopwatch = Stopwatch.StartNew();
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            EnforceRateLimitAsync(read, CancellationToken.None).GetAwaiter().GetResult();
            return read;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            await EnforceRateLimitAsync(read, cancellationToken);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            await EnforceRateLimitAsync(read, cancellationToken);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private async Task EnforceRateLimitAsync(int bytesRead, CancellationToken cancellationToken)
        {
            if (bytesRead <= 0)
            {
                return;
            }

            _totalBytesRead += bytesRead;

            var targetElapsedSeconds = (double)_totalBytesRead / _bytesPerSecond;
            var targetElapsed = TimeSpan.FromSeconds(targetElapsedSeconds);
            var delay = targetElapsed - _stopwatch.Elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
