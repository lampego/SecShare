using SecShare.Console;
using SecShare.Console.Models.Download;
using SecShare.Console.Services.Download;

namespace SecShare.Tests.Unit.Console;

public sealed class DecryptionKeyResolverTests
{
    [Fact]
    public void Resolve_WhenLinkContainsKey_ReturnsKeyWithoutPrompting()
    {
        var reader = new TestDecryptionKeyReader("prompt-key");
        var resolver = new DecryptionKeyResolver(reader);
        var link = CreateLink("fragment-key");

        var result = resolver.Resolve(link);

        Assert.Equal("fragment-key", result);
        Assert.False(reader.HasBeenCalled);
    }

    [Fact]
    public void Resolve_WhenLinkDoesNotContainKey_AsksReader()
    {
        var reader = new TestDecryptionKeyReader("prompt-key");
        var resolver = new DecryptionKeyResolver(reader);
        var link = CreateLink(encryptionKey: null);

        var result = resolver.Resolve(link);

        Assert.Equal("prompt-key", result);
        Assert.True(reader.HasBeenCalled);
    }

    private static SecShareDownloadLink CreateLink(string? encryptionKey)
        => new(
            new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ShareFilesPath}/file-token"),
            new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/file-token"),
            "file-token",
            encryptionKey
        );

    private sealed class TestDecryptionKeyReader(string key) : IDecryptionKeyReader
    {
        public bool HasBeenCalled { get; private set; }

        public string ReadDecryptionKey()
        {
            HasBeenCalled = true;

            return key;
        }
    }
}
