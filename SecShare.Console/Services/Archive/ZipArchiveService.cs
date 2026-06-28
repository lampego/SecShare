using System.IO.Compression;
using System.Text;
using SecShare.Console.Models.Archive;

namespace SecShare.Console.Services.Archive;

public sealed class ZipArchiveService : IZipArchiveService
{
    public const long MaxSourceSizeBytes = 200L * 1024 * 1024;

    public async Task<ZipArchiveBuildResult> CreateFromPathAsync(
        string path,
        CancellationToken cancellationToken)
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
            if (directory.Parent is null)
            {
                throw new InvalidOperationException("A file system root cannot be archived.");
            }

            var rootEntryName = NormalizeEntryName(directory.Name);
            var items = directory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => new ArchiveFileItem(
                    file.FullName,
                    $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, file.FullName))}",
                    file.Length))
                .OrderBy(item => item.EntryName, StringComparer.Ordinal)
                .ToArray();
            var directoryEntryNames = directory
                .EnumerateDirectories("*", SearchOption.AllDirectories)
                .Select(item =>
                    $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, item.FullName))}")
                .Order(StringComparer.Ordinal)
                .ToArray();

            ValidateTotalSize(items);

            var archiveBytes = await CreateArchiveAsync(
                items,
                cancellationToken,
                rootEntryName,
                directoryEntryNames);
            return new ZipArchiveBuildResult(
                archiveBytes,
                items.Sum(item => item.SizeBytes),
                items.Length,
                directory.Name);
        }

        throw new FileNotFoundException($"Path '{path}' does not exist.", path);
    }

    public async Task<ZipArchiveBuildResult> CreateFromTextAsync(
        string text,
        CancellationToken cancellationToken)
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

    public async Task<ZipArchiveExtractResult> ExtractAsync(
        byte[] archiveBytes,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destinationRoot = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(destinationRoot);

        var stagingPath = Path.Combine(destinationRoot, $".secshare-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingPath);

        try
        {
            await using var stream = new MemoryStream(archiveBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entries = ValidateEntries(archive, stagingPath);

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var targetPath = GetSafeTargetPath(stagingPath, entry.FullName);
                if (IsDirectory(entry))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                await using var source = await entry.OpenAsync(cancellationToken);
                await using var target = new FileStream(
                    targetPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);
                await source.CopyToAsync(target, cancellationToken);
            }

            var extractedPaths = MoveStagedItems(stagingPath, destinationRoot);
            return new ZipArchiveExtractResult(
                extractedPaths,
                entries.Where(entry => !IsDirectory(entry)).Sum(entry => entry.Length),
                entries.Count(entry => !IsDirectory(entry)));
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }
        }
    }

    public async Task<string> ReadTextAsync(byte[] archiveBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);

        await using var stream = new MemoryStream(archiveBytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var fileEntries = archive.Entries
            .Where(entry => !IsDirectory(entry))
            .ToArray();

        if (fileEntries.Length != 1)
        {
            throw new InvalidDataException("Text archive must contain exactly one file.");
        }

        var entry = fileEntries[0];
        if (entry.Length > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Text content size must not exceed 200 MB.");
        }

        await using var entryStream = await entry.OpenAsync(cancellationToken);
        using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<byte[]> CreateArchiveAsync(
        IReadOnlyCollection<ArchiveFileItem> items,
        CancellationToken cancellationToken,
        string? rootEntryName = null,
        IReadOnlyCollection<string>? directoryEntryNames = null)
    {
        await using var stream = new MemoryStream();
        await using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (rootEntryName is not null)
            {
                archive.CreateEntry($"{rootEntryName}/");
            }

            foreach (var directoryEntryName in directoryEntryNames ?? [])
            {
                archive.CreateEntry($"{directoryEntryName}/");
            }

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

    private static ZipArchiveEntry[] ValidateEntries(ZipArchive archive, string stagingPath)
    {
        var entries = archive.Entries.ToArray();
        var extractedSizeBytes = entries
            .Where(entry => !IsDirectory(entry))
            .Sum(entry => entry.Length);

        if (extractedSizeBytes > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Extracted archive size must not exceed 200 MB.");
        }

        var targetPaths = new HashSet<string>(GetPathComparer());
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                throw new InvalidDataException("ZIP archive contains an entry with an empty name.");
            }

            var targetPath = GetSafeTargetPath(stagingPath, entry.FullName);
            if (!targetPaths.Add(targetPath))
            {
                throw new InvalidDataException($"ZIP archive contains duplicate entry '{entry.FullName}'.");
            }
        }

        return entries;
    }

    private static IReadOnlyCollection<string> MoveStagedItems(string stagingPath, string destinationRoot)
    {
        var items = new DirectoryInfo(stagingPath)
            .EnumerateFileSystemInfos()
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        foreach (var item in items)
        {
            var destination = Path.Combine(destinationRoot, item.Name);
            if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new IOException($"Destination '{destination}' already exists.");
            }
        }

        foreach (var item in items)
        {
            var destination = Path.Combine(destinationRoot, item.Name);
            if (item is DirectoryInfo)
            {
                Directory.Move(item.FullName, destination);
            }
            else
            {
                File.Move(item.FullName, destination);
            }
        }

        return items
            .Select(item => Path.Combine(destinationRoot, item.Name))
            .ToArray();
    }

    private static string GetSafeTargetPath(string rootPath, string entryName)
    {
        var rootWithSeparator = Path.EndsInDirectorySeparator(rootPath)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        var targetPath = Path.GetFullPath(Path.Combine(rootPath, entryName));

        if (!targetPath.StartsWith(rootWithSeparator, GetPathComparison()))
        {
            throw new InvalidDataException($"ZIP entry '{entryName}' is outside the destination directory.");
        }

        return targetPath;
    }

    private static bool IsDirectory(ZipArchiveEntry entry)
        => entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

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

    private static StringComparison GetPathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static StringComparer GetPathComparer()
        => OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

}
