using System.Text.Json;
using System.Text.Json.Serialization;
using SecShare.Console.Services.Archive;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient(HttpClient httpClient) : ISecShareHttpClient
{
    public const long MaxEncryptedPayloadSizeBytes =
        ZipArchiveService.MaxSourceSizeBytes + (10L * 1024 * 1024);

    private readonly HttpClient _httpClient = httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
