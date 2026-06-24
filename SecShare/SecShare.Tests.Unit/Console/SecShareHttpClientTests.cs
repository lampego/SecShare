using System.Net;
using System.Text;
using SecShare.Console;
using SecShare.Console.Services.Http;

namespace SecShare.Tests.Unit.Console;

public sealed class SecShareHttpClientTests
{
    [Fact]
    public async Task UploadAsync_SendsMultipartRequestAndReportsPayloadProgress()
    {
        var encryptedPayload = new byte[200_000];
        Random.Shared.NextBytes(encryptedPayload);
        var progress = new List<TransferProgress>();
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}"),
                request.RequestUri);
            Assert.IsType<MultipartFormDataContent>(request.Content);

            _ = await request.Content.ReadAsByteArrayAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"token":"file-token"}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var result = await client.UploadAsync(
            encryptedPayload,
            new UploadHttpOptions("24h", 1, false, "source"),
            progress.Add,
            CancellationToken.None);

        Assert.Equal("file-token", result.Token);
        Assert.NotEmpty(progress);
        Assert.Equal(encryptedPayload.LongLength, progress[^1].BytesTransferred);
        Assert.Equal(encryptedPayload.LongLength, progress[^1].TotalBytes);
        Assert.True(progress[^1].BytesPerSecond >= 0);
    }

    [Fact]
    public async Task DownloadAsync_SendsGetRequestAndReportsPayloadProgress()
    {
        var expectedPayload = new byte[200_000];
        Random.Shared.NextBytes(expectedPayload);
        var progress = new List<TransferProgress>();
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/token"),
                request.RequestUri);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedPayload),
            });
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var payload = await client.DownloadAsync(
            new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/token"),
            progress.Add,
            CancellationToken.None);

        Assert.Equal(expectedPayload, payload);
        Assert.NotEmpty(progress);
        Assert.Equal(expectedPayload.LongLength, progress[^1].BytesTransferred);
        Assert.Equal(expectedPayload.LongLength, progress[^1].TotalBytes);
        Assert.True(progress[^1].BytesPerSecond >= 0);
    }

    [Fact]
    public async Task DownloadAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync(
                new Uri($"{SecShareConstants.ServiceBaseUrl}{SecShareConstants.ApiFilesPath}/missing"),
                progress: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_WhenResponseTokenIsEmpty_ThrowsInvalidOperationException()
    {
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            _ = await request.Content!.ReadAsByteArrayAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"token":""}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(
                "payload"u8.ToArray(),
                new UploadHttpOptions("24h", 1, false, "source"),
                progress: null,
                CancellationToken.None));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = SecShareConstants.ServiceBaseUri,
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
