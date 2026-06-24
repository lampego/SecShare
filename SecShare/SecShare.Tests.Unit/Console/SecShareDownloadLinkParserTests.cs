using SecShare.Console.Services.Download;

namespace SecShare.Tests.Unit.Console;

public sealed class SecShareDownloadLinkParserTests
{
    private readonly SecShareDownloadLinkParser parser = new();

    [Fact]
    public void Parse_WithValidShareUrl_ReturnsPayloadUrlAndEncryptionKey()
    {
        var result = this.parser.Parse("https://secshare.me/f/file-token#base64UrlKey");

        Assert.Equal(new Uri("https://secshare.me/api/files/file-token"), result.PayloadUri);
        Assert.Equal("base64UrlKey", result.EncryptionKey);
    }

    [Theory]
    [InlineData("https://secshare.me/f/file-token")]
    [InlineData("https://secshare.me/other/file-token#key")]
    [InlineData("not-a-url")]
    public void Parse_WithInvalidShareUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => this.parser.Parse(url));
    }
}
