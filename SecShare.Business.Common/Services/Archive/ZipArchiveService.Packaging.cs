using System.IO.Compression;
using System.Text;
using SecShare.Business.Common.Models.Archive;

namespace SecShare.Business.Common.Services.Archive;

public sealed partial class ZipArchiveService : IZipArchiveService
{
    public const long MaxSourceSizeBytes = 200L * 1024 * 1024;

    public Task<ZipArchiveBuildResult> CreateFromPathAsync(
        string path,
        CancellationToken cancellationToken
    )
        => this.CreateFromPathsAsync([path], cancellationToken);

    public async Task<ZipArchiveBuildResult> CreateFromPathsAsync(
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new InvalidOperationException("At least one file, directory, or file mask must be specified.");
        }

        var items = new List<ArchiveFileItem>();
        var directoryEntryNames = new List<string>();
        var includedFilePaths = new HashSet<string>(GetPathComparer());
        var includedDirectoryPaths = new HashSet<string>(GetPathComparer());

        foreach (var path in paths)
        {
            AddPathItems(
                path,
                items,
                directoryEntryNames,
                includedFilePaths,
                includedDirectoryPaths
            );
        }

        if (items.Count == 0 && directoryEntryNames.Count == 0)
        {
            throw new FileNotFoundException("The upload mask did not match any files.");
        }

        var uniqueItems = CreateUniquePathItems(items, directoryEntryNames);
        ValidateTotalSize(uniqueItems);

        var archiveBytes = await CreateArchiveAsync(
            uniqueItems,
            cancellationToken,
            directoryEntryNames
        );
        return new ZipArchiveBuildResult(
            archiveBytes,
            uniqueItems.Sum(item => item.SizeBytes),
            uniqueItems.Length,
            ResolveSourceName(paths, uniqueItems)
        );
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

    private static void AddPathItems(
        string path,
        ICollection<ArchiveFileItem> items,
        ICollection<string> directoryEntryNames,
        ISet<string> includedFilePaths,
        ISet<string> includedDirectoryPaths
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
        {
            AddFileItem(new FileInfo(path), file => file.Name, items, includedFilePaths);
            return;
        }

        if (Directory.Exists(path))
        {
            AddDirectoryItems(
                new DirectoryInfo(path),
                items,
                directoryEntryNames,
                includedFilePaths,
                includedDirectoryPaths
            );
            return;
        }

        if (!ContainsWildcard(path))
        {
            throw new FileNotFoundException($"Path '{path}' does not exist.", path);
        }

        var matches = ResolvePattern(path).ToArray();
        if (matches.Length == 0)
        {
            throw new FileNotFoundException("The upload mask did not match any files.", path);
        }

        foreach (var file in matches)
        {
            AddFileItem(file, item => item.Name, items, includedFilePaths);
        }
    }

    private static void AddDirectoryItems(
        DirectoryInfo directory,
        ICollection<ArchiveFileItem> items,
        ICollection<string> directoryEntryNames,
        ISet<string> includedFilePaths,
        ISet<string> includedDirectoryPaths
    )
    {
        if (directory.Parent is null)
        {
            throw new InvalidOperationException("A file system root cannot be archived.");
        }

        if (!includedDirectoryPaths.Add(directory.FullName))
        {
            return;
        }

        var rootEntryName = CreateUniqueEntryName(
            directory.Name,
            new HashSet<string>(directoryEntryNames, StringComparer.OrdinalIgnoreCase)
        );
        directoryEntryNames.Add(rootEntryName);

        foreach (var childDirectory in directory
                     .EnumerateDirectories("*", SearchOption.AllDirectories)
                     .OrderBy(item => item.FullName, StringComparer.Ordinal))
        {
            directoryEntryNames.Add(
                $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, childDirectory.FullName))}"
            );
        }

        foreach (var file in directory
                     .EnumerateFiles("*", SearchOption.AllDirectories)
                     .OrderBy(item => item.FullName, StringComparer.Ordinal))
        {
            AddFileItem(
                file,
                item => $"{rootEntryName}/{NormalizeEntryName(Path.GetRelativePath(directory.FullName, item.FullName))}",
                items,
                includedFilePaths
            );
        }
    }

    private static void AddFileItem(
        FileInfo file,
        Func<FileInfo, string> getEntryName,
        ICollection<ArchiveFileItem> items,
        ISet<string> includedFilePaths
    )
    {
        if (!includedFilePaths.Add(file.FullName))
        {
            return;
        }

        items.Add(CreatePathItem(file.FullName, getEntryName(file), file.Length));
    }

    private static IEnumerable<FileInfo> ResolvePattern(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var searchDirectory = string.IsNullOrEmpty(directory) ? "." : directory;
        var searchPattern = Path.GetFileName(path);

        if (!Directory.Exists(searchDirectory))
        {
            throw new DirectoryNotFoundException($"Directory '{searchDirectory}' does not exist.");
        }

        return Directory
            .EnumerateFiles(searchDirectory, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(file => new FileInfo(file));
    }

    private static string ResolveSourceName(
        IReadOnlyCollection<string> paths,
        IReadOnlyCollection<ArchiveFileItem> items
    )
    {
        if (paths.Count == 1 && !ContainsWildcard(paths.First()))
        {
            return Path.GetFileName(Path.TrimEndingDirectorySeparator(paths.First()));
        }

        return items.Count == 1
            ? Path.GetFileName(items.First().EntryName)
            : "selected files";
    }

    private static async Task<byte[]> CreateArchiveAsync(
        IReadOnlyCollection<ArchiveFileItem> items,
        CancellationToken cancellationToken,
        IReadOnlyCollection<string>? directoryEntryNames = null
    )
    {
        await using var stream = new MemoryStream();
        await using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
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

    private static ArchiveFileItem[] CreateUniquePathItems(
        IReadOnlyCollection<ArchiveFileItem> sourceItems,
        IReadOnlyCollection<string> directoryEntryNames
    )
    {
        var usedNames = new HashSet<string>(directoryEntryNames, StringComparer.OrdinalIgnoreCase);

        return sourceItems
            .Select(item => new ArchiveFileItem(
                CreateUniqueEntryName(item.EntryName, usedNames),
                item.SizeBytes,
                item.OpenReadStreamAsync
            ))
            .ToArray();
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
                    CreateUniqueEntryName(NormalizeSingleFileEntryName(item.EntryName), usedNames),
                    item.SizeBytes,
                    item.OpenReadStreamAsync
                );
            })
            .ToArray();
    }

    private static string CreateUniqueEntryName(string entryName, ISet<string> usedNames)
    {
        var normalizedName = NormalizeEntryName(entryName).Trim('/');
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = "file";
        }

        if (usedNames.Add(normalizedName))
        {
            return normalizedName;
        }

        var separatorIndex = normalizedName.LastIndexOf('/');
        var directoryName = separatorIndex < 0 ? string.Empty : normalizedName[..(separatorIndex + 1)];
        var fileName = normalizedName[(separatorIndex + 1)..];
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 2; ; index++)
        {
            var candidate = $"{directoryName}{nameWithoutExtension}-{index}{extension}";
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

    private static bool ContainsWildcard(string path)
        => path.Contains('*') || path.Contains('?');
}
