using SecShare.Business.Common.Http;
using SecShare.Business.Common.Services.Archive;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient(HttpClient httpClient)
    : ISecShareHttpClient, ISecShareDownloadClient
{
    public const long MaxEncryptedPayloadSizeBytes =
        ZipArchiveService.MaxSourceSizeBytes + (10L * 1024 * 1024);

    private readonly HttpClient _httpClient = httpClient;
}
