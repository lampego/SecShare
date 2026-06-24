namespace SecShare.Console.Services.Download;

public interface ISecShareDownloadLinkParser
{
    SecShareDownloadLink Parse(string url);
}
