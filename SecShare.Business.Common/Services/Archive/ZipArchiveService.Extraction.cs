using System.IO.Compression;
using System.Text;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Models.Archive;

namespace SecShare.Business.Common.Services.Archive;

public sealed partial class ZipArchiveService
{
    public IReadOnlyCollection<string> GetConflictingPaths(byte[] archiveBytes, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(archiveBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destinationRoot = Path.GetFullPath(destinationPath);
        if (!Directory.Exists(destinationRoot))
        {
            return [];
        }

        using var stream = new MemoryStream(archiveBytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entries = ValidateEntries(archive);

        return GetConflictingPaths(entries, destinationRoot);
    }

    public async Task<ZipArchiveExtractResult> ExtractAsync(
        byte[] archiveBytes,
        string destinationPath,
        CancellationToken cancellationToken,
        ZipArchiveExtractOptions? options = null
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

            var extractedPaths = MoveStagedItems(
                stagingPath,
                destinationRoot,
                options ?? new ZipArchiveExtractOptions()
            );
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

    private static IReadOnlyCollection<string> GetConflictingPaths(
        IReadOnlyCollection<ZipArchiveEntry> entries,
        string destinationRoot
    )
    {
        return GetArchiveRootItemNames(entries)
            .Select(itemName => Path.Combine(destinationRoot, itemName))
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();
    }

    private static IReadOnlyCollection<string> MoveStagedItems(
        string stagingPath,
        string destinationRoot,
        ZipArchiveExtractOptions options
    )
    {
        var items = new DirectoryInfo(stagingPath)
            .EnumerateFileSystemInfos()
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var conflictingPaths = items
            .Select(item => Path.Combine(destinationRoot, item.Name))
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();

        if (conflictingPaths.Length > 0 && !options.IsOverwriteEnabled)
        {
            throw new IOException($"Destination already contains: {string.Join(", ", conflictingPaths)}.");
        }

        foreach (var conflictingPath in conflictingPaths)
        {
            if (Directory.Exists(conflictingPath))
            {
                Directory.Delete(conflictingPath, recursive: true);
                continue;
            }

            File.Delete(conflictingPath);
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

    private static IReadOnlyCollection<string> GetArchiveRootItemNames(IReadOnlyCollection<ZipArchiveEntry> entries)
    {
        var rootItemNames = new HashSet<string>(GetPathComparer());
        foreach (var entry in entries)
        {
            var rootItemName = NormalizeEntryName(entry.FullName)
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rootItemName))
            {
                rootItemNames.Add(rootItemName);
            }
        }

        return rootItemNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsDirectory(ZipArchiveEntry entry)
        => entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

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
