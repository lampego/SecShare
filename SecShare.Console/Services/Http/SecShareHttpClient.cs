using SecShare.Business.Common.Http;
using SecShare.Business.Common.Http.Clients;
using SecShare.Business.Common.Services.Archive;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient(HttpClient httpClient)
    : ISecShareHttpClient, IDownloadClient, IUploadClient
{
    public const long MaxEncryptedPayloadSizeBytes =
        ZipArchiveService.MaxSourceSizeBytes + (10L * 1024 * 1024);

    private readonly HttpClient _httpClient = httpClient;
}
