using SecShare.Console;
using SecShare.Console.Services.Download;

namespace SecShare.Tests.Unit.Console;

public sealed class SecShareDownloadLinkParserTests
{
    private readonly SecShareDownloadLinkParser parser = new();

    [Fact]
    public void Parse_WithValidShareUrlAndKey_ReturnsPayloadUrlFileIdAndEncryptionKey()
    {
        var result = this.parser.Parse(
            $"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ShareFilesPath}/file-token#base64UrlKey"
        );

        Assert.Equal(
            new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/file-token"),
            result.PayloadUri
        );
        Assert.Equal("file-token", result.FileId);
        Assert.Equal("base64UrlKey", result.EncryptionKey);
    }

    [Fact]
    public void Parse_WithValidShareUrlWithoutKey_ReturnsPayloadUrlFileIdAndNoEncryptionKey()
    {
        var result = this.parser.Parse(
            $"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ShareFilesPath}/file-token"
        );

        Assert.Equal(
            new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/file-token"),
            result.PayloadUri
        );
        Assert.Equal("file-token", result.FileId);
        Assert.Null(result.EncryptionKey);
    }

    [Theory]
    [InlineData("https://secshare.me/other/file-token#key")]
    [InlineData("not-a-url")]
    public void Parse_WithInvalidShareUrl_ThrowsArgumentException(string url)
    {
        var exception = Assert.Throws<ArgumentException>(() => this.parser.Parse(url));

        Assert.NotEmpty(exception.Message);
    }
}
