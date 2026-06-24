using System.Net;
using SecShare.Console.Services.Download;

namespace SecShare.Tests.Unit.Console;

public sealed class DownloadHttpClientTests
{
    [Fact]
    public async Task DownloadAsync_SendsGetRequestAndReturnsPayload()
    {
        var expectedPayload = "encrypted payload"u8.ToArray();
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(new Uri("https://secshare.me/api/files/token"), request.RequestUri);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedPayload),
            });
        });
        using var httpClient = new HttpClient(handler);
        var client = new DownloadHttpClient(httpClient);

        var payload = await client.DownloadAsync(
            new Uri("https://secshare.me/api/files/token"),
            CancellationToken.None);

        Assert.Equal(expectedPayload, payload);
    }

    [Fact]
    public async Task DownloadAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var httpClient = new HttpClient(handler);
        var client = new DownloadHttpClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync(
                new Uri("https://secshare.me/api/files/missing"),
                CancellationToken.None));
    }

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
