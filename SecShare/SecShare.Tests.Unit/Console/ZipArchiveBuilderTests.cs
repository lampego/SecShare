using System.IO.Compression;
using SecShare.Console.Services.Archive;

namespace SecShare.Tests.Unit.Console;

public sealed class ZipArchiveBuilderTests
{
    private readonly ZipArchiveBuilder zipArchiveBuilder = new();

    [Fact]
    public async Task CreateFromPathAsync_WithDirectory_CreatesZipWithRelativeFileEntries()
    {
        var directory = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "nested"));
            await File.WriteAllTextAsync(Path.Combine(directory, "root.txt"), "root");
            await File.WriteAllTextAsync(Path.Combine(directory, "nested", "child.txt"), "child");

            var result = await this.zipArchiveBuilder.CreateFromPathAsync(directory, CancellationToken.None);

            using var stream = new MemoryStream(result.ArchiveBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entries = archive.Entries.Select(entry => entry.FullName).Order().ToArray();

            Assert.Equal(2, result.FileCount);
            Assert.Equal(["nested/child.txt", "root.txt"], entries);
            Assert.True(result.SourceSizeBytes > 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
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
                stream.SetLength(ZipArchiveBuilder.MaxSourceSizeBytes + 1);
            }

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.zipArchiveBuilder.CreateFromPathAsync(directory, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"secshare-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }
}
