using System.Net;
using SecShare.Business.Common.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private sealed class ProgressByteArrayContent(
        byte[] content,
        Action<TransferProgress>? progress)
        : HttpContent
    {
        private const int BufferSize = 81920;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => this.SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken)
        {
            var tracker = new TransferProgressTracker(content.LongLength, progress);

            for (var offset = 0; offset < content.Length; offset += BufferSize)
            {
                var count = Math.Min(BufferSize, content.Length - offset);
                await stream.WriteAsync(content.AsMemory(offset, count), cancellationToken);
                tracker.Report(count);
            }

            tracker.Complete();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.LongLength;
            return true;
        }
    }
}
