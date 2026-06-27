using SecShare.Console;
using SecShare.Console.Services.Upload;

namespace SecShare.Tests.Unit.Console;

public sealed class UploadShareLinkBuilderTests
{
    [Fact]
    public void Create_BuildsFullSecureLinkWithFragmentAndSeparateParts()
    {
        var links = UploadShareLinkBuilder.Create("file-token", "base64UrlKey");

        Assert.Equal(
            $"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ShareFilesPath}/file-token#base64UrlKey",
            links.FullSecureLink);
        Assert.Equal(
            $"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ShareFilesPath}/file-token",
            links.Link);
        Assert.Equal("base64UrlKey", links.DecryptionKey);
    }

    [Fact]
    public void Format_IncludesFullLinkSeparateLinkAndDecryptionKey()
    {
        var links = UploadShareLinkBuilder.Create("file-token", "base64UrlKey");

        var output = UploadShareOutputFormatter.Format(links);

        Assert.Contains("Full secure link:", output);
        Assert.Contains(links.FullSecureLink, output);
        Assert.Contains("Separate sharing:", output);
        Assert.Contains("Link:", output);
        Assert.Contains(links.Link, output);
        Assert.Contains("Decryption key:", output);
        Assert.Contains(links.DecryptionKey, output);
        Assert.Contains("Anyone with this full link can decrypt and download the file.", output);
    }
}
