using System.Net.Http.Json;

namespace SecShare.Console.Services.Upload.Http;

public sealed class UploadHttpClient(HttpClient httpClient) : IUploadHttpClient
{
    public async Task<UploadHttpResult> UploadAsync(
        byte[] encryptedPayload,
        UploadHttpOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        ArgumentNullException.ThrowIfNull(options);

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(encryptedPayload), "file", $"{options.SourceName}.secshare" },
            { JsonContent.Create(options), "metadata" },
        };

        using var response = await httpClient.PostAsync("/api/files", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UploadHttpResult>(cancellationToken);
        return result ?? throw new InvalidOperationException("Upload response is empty.");
    }
}
