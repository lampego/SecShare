using System.IO.Compression;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Services.Archive;

namespace SecShare.Tests.Unit.Console;

public sealed class ZipArchiveServiceTests
{
    private readonly ZipArchiveService zipArchiveService = new();

    [Fact]
    public async Task CreateAndExtract_WithFile_RestoresOriginalFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourcePath = Path.Combine(root, "document.txt");
            var destinationPath = Path.Combine(root, "output");
            await File.WriteAllTextAsync(sourcePath, "file content");

            var archive = await this.zipArchiveService.CreateFromPathAsync(
                sourcePath,
                CancellationToken.None
            );
            var result = await this.zipArchiveService.ExtractAsync(
                archive.ArchiveBytes,
                destinationPath,
                CancellationToken.None
            );

            var extractedPath = Path.Combine(destinationPath, "document.txt");
            Assert.Equal("file content", await File.ReadAllTextAsync(extractedPath));
            Assert.Equal([extractedPath], result.ExtractedPaths);
            Assert.Equal(1, result.FileCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAndExtract_WithDirectory_RestoresRootDirectoryAndFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourcePath = Path.Combine(root, "source");
            var destinationPath = Path.Combine(root, "output");
            Directory.CreateDirectory(Path.Combine(sourcePath, "nested"));
            Directory.CreateDirectory(Path.Combine(sourcePath, "empty"));
            await File.WriteAllTextAsync(Path.Combine(sourcePath, "root.txt"), "root");
            await File.WriteAllTextAsync(Path.Combine(sourcePath, "nested", "child.txt"), "child");

            var archive = await this.zipArchiveService.CreateFromPathAsync(
                sourcePath,
                CancellationToken.None
            );
            var result = await this.zipArchiveService.ExtractAsync(
                archive.ArchiveBytes,
                destinationPath,
                CancellationToken.None
            );

            var extractedRoot = Path.Combine(destinationPath, "source");
            Assert.Equal("root", await File.ReadAllTextAsync(Path.Combine(extractedRoot, "root.txt")));
            Assert.Equal("child", await File.ReadAllTextAsync(Path.Combine(extractedRoot, "nested", "child.txt")));
            Assert.True(Directory.Exists(Path.Combine(extractedRoot, "empty")));
            Assert.Equal([extractedRoot], result.ExtractedPaths);
            Assert.Equal(2, result.FileCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFromTextAndReadText_RestoresOriginalTextWithoutExtractingFiles()
    {
        var archive = await this.zipArchiveService.CreateFromTextAsync(
            "secret message",
            CancellationToken.None
        );

        var text = await this.zipArchiveService.ReadTextAsync(
            archive.ArchiveBytes,
            CancellationToken.None
        );

        Assert.Equal("secret message", text);
        Assert.Equal(1, archive.FileCount);
        Assert.Equal("message.txt", archive.SourceName);
    }

    [Fact]
    public async Task ReadContentAsync_WithSingleFile_ReturnsInnerFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourcePath = Path.Combine(root, "document.txt");
            await File.WriteAllTextAsync(sourcePath, "file content");

            var archive = await this.zipArchiveService.CreateFromPathAsync(
                sourcePath,
                CancellationToken.None
            );
            var content = await this.zipArchiveService.ReadContentAsync(
                archive.ArchiveBytes,
                StorageContentType.File,
                CancellationToken.None
            );

            Assert.False(content.IsText);
            Assert.Equal("document.txt", content.FileName);
            Assert.Equal("file content", await ReadTextAsync(content.FileBytes));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReadContentAsync_WithDirectory_ReturnsArchiveWithRootDirectoryName()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourcePath = Path.Combine(root, "source");
            Directory.CreateDirectory(sourcePath);
            await File.WriteAllTextAsync(Path.Combine(sourcePath, "root.txt"), "root");

            var archive = await this.zipArchiveService.CreateFromPathAsync(
                sourcePath,
                CancellationToken.None
            );
            var content = await this.zipArchiveService.ReadContentAsync(
                archive.ArchiveBytes,
                StorageContentType.Folder,
                CancellationToken.None
            );

            Assert.False(content.IsText);
            Assert.Equal("source.zip", content.FileName);
            Assert.Equal(archive.ArchiveBytes, content.FileBytes);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFromPathAsync_WhenTotalSizeExceedsLimit_ThrowsInvalidOperationException()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "too-large.bin");
            await using (var stream = File.Create(path))
            {
                stream.SetLength(ZipArchiveService.MaxSourceSizeBytes + 1);
            }

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.zipArchiveService.CreateFromPathAsync(directory, CancellationToken.None)
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAsync_WithEntryOutsideDestination_ThrowsInvalidDataException()
    {
        var destination = CreateTempDirectory();
        try
        {
            byte[] archiveBytes;
            await using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var entry = archive.CreateEntry("../outside.txt");
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync("malicious"u8.ToArray());
                }

                archiveBytes = stream.ToArray();
            }

            await Assert.ThrowsAsync<InvalidDataException>(
                () => this.zipArchiveService.ExtractAsync(
                    archiveBytes,
                    destination,
                    CancellationToken.None
                )
            );

            Assert.False(File.Exists(Path.Combine(destination, "outside.txt")));
        }
        finally
        {
            Directory.Delete(destination, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"secshare-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }

    private static async Task<string> ReadTextAsync(byte[]? bytes)
    {
        Assert.NotNull(bytes);

        return await new StreamReader(new MemoryStream(bytes)).ReadToEndAsync();
    }
}
