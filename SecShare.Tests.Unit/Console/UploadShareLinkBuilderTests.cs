using SecShare.Console;
using SecShare.Console.Models.Upload;
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
    public void Format_IncludesCompactUploadResultWithSeparateKeyBlockLast()
    {
        var package = new UploadPackage(
            new byte[2_898],
            "base64UrlKey",
            2_755,
            2_800,
            1,
            "map_pin.png"
        );
        var links = UploadShareLinkBuilder.Create(
            "019f09f3-d318-77fd-9ca2-ba2bd0223566",
            "EqHNg7cdhvwJENCL8EmjqckHnGyqb4n_MyiOp9Q58k-k"
        );

        var output = UploadShareOutputFormatter.Format(
            package,
            links,
            "file",
            "60d",
            1
        );
        var lines = output.Split(Environment.NewLine);

        Assert.Equal(
            [
                "Upload completed",
                "Details: Source: map_pin.png | Mode: file | Files: 1",
                "Size: 2.69 KB -> 2.83 KB encrypted | Expires: 60d | Downloads: 1",
                "Download command:",
                $"secshare get \"{links.FullSecureLink}\"",
                "Sharing options:",
                "1. Full secure link - Anyone with this link can decrypt and download the file.",
                links.FullSecureLink,
                "2. Separate link and decryption key - Send the link in chat. Share the key separately.",
                "Link:",
                links.Link,
                "Decryption key:",
                links.DecryptionKey
            ],
            lines
        );
    }
}
