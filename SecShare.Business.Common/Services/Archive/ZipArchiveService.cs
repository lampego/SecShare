using System.IO.Compression;
using System.Text;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Models.Archive;

namespace SecShare.Business.Common.Services.Archive;

public sealed class ZipArchiveService : IZipArchiveService
{
    public const long MaxSourceSizeBytes = 200L * 1024 * 1024;

    public async Task<ZipArchiveBuildResult> CreateFromPathAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
        {
            var file = new FileInfo(path);
            var item = CreatePathItem(file.FullName, file.Name, file.Length);

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
                .Select(file => CreatePathItem(
                    file.FullName,
                    $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, file.FullName))}",
                    file.Length
                ))
                .OrderBy(item => item.EntryName, StringComparer.Ordinal)
                .ToArray();
            var directoryEntryNames = directory
                .EnumerateDirectories("*", SearchOption.AllDirectories)
                .Select(item =>
                    $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, item.FullName))}"
                )
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            ValidateTotalSize(items);

            var archiveBytes = await CreateArchiveAsync(
                items,
                cancellationToken,
                rootEntryName,
                directoryEntryNames
            );
            return new ZipArchiveBuildResult(
                archiveBytes,
                items.Sum(item => item.SizeBytes),
                items.Length,
                directory.Name
            );
        }

        throw new FileNotFoundException($"Path '{path}' does not exist.", path);
    }

    public async Task<ZipArchiveBuildResult> CreateFromStreamsAsync(
        IReadOnlyCollection<ZipArchiveSourceItem> items,
        string sourceName,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one file must be selected.");
        }

        var archiveItems = CreateUniqueStreamItems(items);
        ValidateTotalSize(archiveItems);

        var archiveBytes = await CreateArchiveAsync(archiveItems, cancellationToken);
        return new ZipArchiveBuildResult(
            archiveBytes,
            archiveItems.Sum(item => item.SizeBytes),
            archiveItems.Length,
            sourceName
        );
    }

    public async Task<ZipArchiveBuildResult> CreateFromTextAsync(
        string text,
        CancellationToken cancellationToken
    )
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
        CancellationToken cancellationToken
    )
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
                    useAsync: true
                );
                await source.CopyToAsync(target, cancellationToken);
            }

            var extractedPaths = MoveStagedItems(stagingPath, destinationRoot);
            return new ZipArchiveExtractResult(
                extractedPaths,
                entries.Where(entry => !IsDirectory(entry)).Sum(entry => entry.Length),
                entries.Count(entry => !IsDirectory(entry))
            );
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
        var entries = ValidateEntries(archive);

        return await ReadTextAsync(entries, cancellationToken);
    }

    public async Task<ZipArchiveContentResult> ReadContentAsync(
        byte[] archiveBytes,
        StorageContentType contentType,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);

        await using var stream = new MemoryStream(archiveBytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entries = ValidateEntries(archive);

        if (contentType == StorageContentType.Text)
        {
            var text = await ReadTextAsync(entries, cancellationToken);
            return new ZipArchiveContentResult(text, null, null);
        }

        var fileEntries = entries
            .Where(entry => !IsDirectory(entry))
            .ToArray();

        if (contentType == StorageContentType.File && fileEntries.Length == 1)
        {
            var entry = fileEntries[0];
            var entryName = ResolveFileEntryName(entry);
            await using var target = new MemoryStream();
            await using (var source = await entry.OpenAsync(cancellationToken))
            {
                await source.CopyToAsync(target, cancellationToken);
            }

            return new ZipArchiveContentResult(null, target.ToArray(), entryName);
        }

        return new ZipArchiveContentResult(
            null,
            archiveBytes,
            ResolveArchiveFileName(entries, contentType)
        );
    }

    private static async Task<byte[]> CreateArchiveAsync(
        IReadOnlyCollection<ArchiveFileItem> items,
        CancellationToken cancellationToken,
        string? rootEntryName = null,
        IReadOnlyCollection<string>? directoryEntryNames = null
    )
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
                await using var sourceStream = await item.OpenReadStreamAsync(cancellationToken);
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await sourceStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        return stream.ToArray();
    }

    private static ZipArchiveEntry[] ValidateEntries(ZipArchive archive, string? stagingPath = null)
    {
        var entries = archive.Entries.ToArray();
        var extractedSizeBytes = entries
            .Where(entry => !IsDirectory(entry))
            .Sum(entry => entry.Length);

        if (extractedSizeBytes > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Extracted archive size must not exceed 200 MB.");
        }

        var entryNames = new HashSet<string>(StringComparer.Ordinal);
        var targetPaths = stagingPath is null
            ? null
            : new HashSet<string>(GetPathComparer());
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                throw new InvalidDataException("ZIP archive contains an entry with an empty name.");
            }

            if (!entryNames.Add(NormalizeEntryName(entry.FullName).TrimEnd('/')))
            {
                throw new InvalidDataException($"ZIP archive contains duplicate entry '{entry.FullName}'.");
            }

            if (stagingPath is null || targetPaths is null)
            {
                continue;
            }

            var targetPath = GetSafeTargetPath(stagingPath, entry.FullName);
            if (!targetPaths.Add(targetPath))
            {
                throw new InvalidDataException($"ZIP archive contains duplicate entry '{entry.FullName}'.");
            }
        }

        return entries;
    }

    private static async Task<string> ReadTextAsync(
        IReadOnlyCollection<ZipArchiveEntry> entries,
        CancellationToken cancellationToken
    )
    {
        var fileEntries = entries
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

    private static string ResolveArchiveFileName(
        IReadOnlyCollection<ZipArchiveEntry> entries,
        StorageContentType contentType
    )
    {
        if (contentType == StorageContentType.Folder)
        {
            var rootDirectoryName = ResolveRootDirectoryName(entries);
            if (!string.IsNullOrWhiteSpace(rootDirectoryName))
            {
                return $"{rootDirectoryName}.zip";
            }
        }

        return "secshare-files.zip";
    }

    private static string? ResolveRootDirectoryName(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        var explicitRootDirectory = entries
            .Where(IsDirectory)
            .Select(entry => NormalizeEntryName(entry.FullName).TrimEnd('/'))
            .FirstOrDefault(name => !name.Contains('/'));

        if (!string.IsNullOrWhiteSpace(explicitRootDirectory))
        {
            return explicitRootDirectory;
        }

        var rootNames = entries
            .Select(entry => NormalizeEntryName(entry.FullName).Trim('/'))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Split('/')[0])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return rootNames.Length == 1
            ? rootNames[0]
            : null;
    }

    private static string ResolveFileEntryName(ZipArchiveEntry entry)
    {
        var entryName = NormalizeEntryName(entry.FullName)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrEmpty(entryName)
            ? "secshare-file"
            : entryName;
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

    private static ArchiveFileItem CreatePathItem(string fullPath, string entryName, long sizeBytes)
    {
        return new ArchiveFileItem(
            entryName,
            sizeBytes,
            _ => ValueTask.FromResult<Stream>(File.OpenRead(fullPath))
        );
    }

    private static ArchiveFileItem[] CreateUniqueStreamItems(IReadOnlyCollection<ZipArchiveSourceItem> sourceItems)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return sourceItems
            .Select(item =>
            {
                ArgumentNullException.ThrowIfNull(item);
                if (item.SizeBytes < 0)
                {
                    throw new InvalidOperationException("File size must not be negative.");
                }

                return new ArchiveFileItem(
                    CreateUniqueEntryName(item.EntryName, usedNames),
                    item.SizeBytes,
                    item.OpenReadStreamAsync
                );
            })
            .ToArray();
    }

    private static string CreateUniqueEntryName(string fileName, ISet<string> usedNames)
    {
        var entryName = NormalizeSingleFileEntryName(fileName);
        if (string.IsNullOrWhiteSpace(entryName))
        {
            entryName = "file";
        }

        if (usedNames.Add(entryName))
        {
            return entryName;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(entryName);
        var extension = Path.GetExtension(entryName);
        for (var index = 2; ; index++)
        {
            var candidate = $"{nameWithoutExtension}-{index}{extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string NormalizeSingleFileEntryName(string entryName)
    {
        var normalizedName = NormalizeEntryName(entryName)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(normalizedName)
            ? string.Empty
            : normalizedName;
    }

    private static string NormalizeEntryName(string entryName)
        => entryName.Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Replace('\\', '/');

    private static StringComparison GetPathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static StringComparer GetPathComparer()
        => OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
