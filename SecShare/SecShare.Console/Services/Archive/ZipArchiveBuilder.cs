using System.IO.Compression;
using System.Text;

namespace SecShare.Console.Services.Archive;

public sealed class ZipArchiveBuilder : IZipArchiveBuilder
{
    public const long MaxSourceSizeBytes = 200L * 1024 * 1024;

    public async Task<ZipArchiveBuildResult> CreateFromPathAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
        {
            var file = new FileInfo(path);
            var item = new ArchiveFileItem(file.FullName, file.Name, file.Length);

            ValidateTotalSize([item]);

            var archiveBytes = await CreateArchiveAsync([item], cancellationToken);
            return new ZipArchiveBuildResult(archiveBytes, item.SizeBytes, 1, file.Name);
        }

        if (Directory.Exists(path))
        {
            var directory = new DirectoryInfo(path);
            var items = directory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => new ArchiveFileItem(
                    file.FullName,
                    NormalizeEntryName(Path.GetRelativePath(directory.FullName, file.FullName)),
                    file.Length))
                .OrderBy(item => item.EntryName, StringComparer.Ordinal)
                .ToArray();

            ValidateTotalSize(items);

            var archiveBytes = await CreateArchiveAsync(items, cancellationToken);
            return new ZipArchiveBuildResult(
                archiveBytes,
                items.Sum(item => item.SizeBytes),
                items.Length,
                directory.Name);
        }

        throw new FileNotFoundException($"Path '{path}' does not exist.", path);
    }

    public async Task<ZipArchiveBuildResult> CreateFromTextAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.LongLength > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Total upload size must not exceed 200 MB.");
        }

        await using var stream = new MemoryStream();
        await using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("message.txt", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(bytes, cancellationToken);
        }

        return new ZipArchiveBuildResult(stream.ToArray(), bytes.LongLength, 1, "message.txt");
    }

    private static async Task<byte[]> CreateArchiveAsync(
        IReadOnlyCollection<ArchiveFileItem> items,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entry = archive.CreateEntry(item.EntryName, CompressionLevel.Optimal);
                await using var sourceStream = File.OpenRead(item.FullPath);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await sourceStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        return stream.ToArray();
    }

    private static void ValidateTotalSize(IReadOnlyCollection<ArchiveFileItem> items)
    {
        var totalSizeBytes = items.Sum(item => item.SizeBytes);
        if (totalSizeBytes > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Total upload size must not exceed 200 MB.");
        }
    }

    private static string NormalizeEntryName(string entryName)
        => entryName.Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

    private sealed record ArchiveFileItem(string FullPath, string EntryName, long SizeBytes);
}
