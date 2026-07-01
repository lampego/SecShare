using System.Net;
using System.Net.Http;
using System.Text;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Exceptions;

namespace SecShare.Tests.Unit.Web;

public sealed class WebSecShareHttpClientTests
{
    private const string BaseUrl = "https://secshare.me";
    private const string ApiFilesPath = "/api/files";

    [Fact]
    public async Task UploadAsync_SetsWebClientTypeHeaderAndSendsMultipartRequest()
    {
        var encryptedPayload = "encrypted payload"u8.ToArray();
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(new Uri($"{BaseUrl}{ApiFilesPath}"), request.RequestUri);
            Assert.True(request.Headers.TryGetValues(SecShareClientHeaders.ClientType, out var values));
            Assert.Equal(SecShareClientHeaders.ClientTypeWeb, values.Single());

            var multipartContent = Assert.IsType<MultipartFormDataContent>(request.Content);
            var formValues = await ReadMultipartFormValuesAsync(multipartContent, cancellationToken);
            Assert.Contains("file", formValues.Keys);
            Assert.Equal("24h", formValues["Options.Expires"]);
            Assert.Equal("1", formValues["Options.Downloads"]);
            Assert.Equal("Text", formValues["Options.ContentType"]);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"token":"file-token"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });

        var client = CreateClient(handler);
        var result = await client.UploadAsync(
            encryptedPayload,
            new UploadFileOptions
            {
                Expires = "24h",
                Downloads = 1,
                ContentType = StorageContentType.Text
            },
            progress: null,
            CancellationToken.None
        );

        Assert.Equal("file-token", result.Token);
    }

    [Fact]
    public async Task DownloadAsync_SetsWebClientTypeHeader()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.True(request.Headers.TryGetValues(SecShareClientHeaders.ClientType, out var values));
            Assert.Equal(SecShareClientHeaders.ClientTypeWeb, values.Single());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("payload"u8.ToArray()),
            }
            );
        });

        var client = CreateClient(handler);
        _ = await client.DownloadAsync("token", progress: null, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAsync_BuildsCorrectRequestUri()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(
                new Uri($"{BaseUrl}{ApiFilesPath}/my-file-id"),
                request.RequestUri
            );

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("payload"u8.ToArray()),
            }
            );
        });

        var client = CreateClient(handler);
        _ = await client.DownloadAsync("my-file-id", progress: null, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsParsedResult()
    {
        var expectedPayload = "encrypted"u8.ToArray();
        var expectedDeleteAt = DateTime.UtcNow;

        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedPayload),
            };
            response.Headers.Add(SecShareFileHeaders.ContentType, StorageContentType.Text.ToString());
            response.Headers.Add(SecShareFileHeaders.FileId, "file-id");
            response.Headers.Add(SecShareFileHeaders.FileExtension, "secshare");
            response.Headers.Add(SecShareFileHeaders.FileSize, "42");
            response.Headers.Add(SecShareFileHeaders.DownloadsRemaining, "3");
            response.Headers.Add(SecShareFileHeaders.DeleteAt, expectedDeleteAt.ToString("O"));
            response.Headers.Add(SecShareFileHeaders.PayloadType, SecShareFileHeaders.EncryptedArchivePayloadType);
            return Task.FromResult(response);
        });

        var client = CreateClient(handler);
        var result = await client.DownloadAsync("token", progress: null, CancellationToken.None);

        Assert.Equal(expectedPayload, result.EncryptedPayload);
        Assert.Equal(StorageContentType.Text, result.ContentType);
        Assert.Equal("file-id", result.FileId);
        Assert.Equal("secshare", result.Extension);
        Assert.Equal(42, result.Size);
        Assert.Equal(3, result.DownloadsRemaining);
        Assert.Equal(expectedDeleteAt, result.DeleteAt);
        Assert.Equal(SecShareFileHeaders.EncryptedArchivePayloadType, result.PayloadType);
    }

    [Fact]
    public async Task DownloadAsync_ReportsProgress()
    {
        var payload = new byte[200_000];
        Random.Shared.NextBytes(payload);
        var progress = new List<TransferProgress>();

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload),
            })
        );

        var client = CreateClient(handler);
        _ = await client.DownloadAsync("token", progress.Add, CancellationToken.None);

        Assert.NotEmpty(progress);
        Assert.Equal(payload.LongLength, progress[^1].BytesTransferred);
        Assert.True(progress[^1].BytesPerSecond >= 0);
    }

    [Fact]
    public async Task DownloadAsync_WhenNotFound_ThrowsHttpRequestExceptionWithStatusCode()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
        );

        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync("missing", progress: null, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DownloadAsync_WhenForbidden_ThrowsHttpRequestExceptionWithStatusCode()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden))
        );

        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync("token", progress: null, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task DownloadAsync_WhenApiReturnsFileNotFoundError_ThrowsDomainException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"status":"fail","message":"Decrypted data is unavailable","errorCode":"FileNotFoundDomainException"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            })
        );

        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<FileNotFoundDomainException>(
            () => client.DownloadAsync("missing", progress: null, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DownloadAsync_WhenApiReturnsDownloadLimitError_ThrowsDomainException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"status":"fail","message":"Decrypted data is unavailable","errorCode":"DownloadLimitExhaustedDomainException"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            })
        );

        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<DownloadLimitExhaustedDomainException>(
            () => client.DownloadAsync("token", progress: null, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DownloadAsync_WhenApiReturnsValidationError_ThrowsApiExceptionWithValidationMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"Id":["Invalid file identifier format."]}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            })
        );

        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => client.DownloadAsync("bad-token", progress: null, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("Id: Invalid file identifier format.", ex.Message);
    }

    private static WebSecShareHttpClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) });

    private static async Task<Dictionary<string, string>> ReadMultipartFormValuesAsync(
        MultipartFormDataContent multipartContent,
        CancellationToken cancellationToken
    )
    {
        var values = new Dictionary<string, string>();
        foreach (var content in multipartContent)
        {
            var name = content.Headers.ContentDisposition?.Name?.Trim('"');
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (content.Headers.ContentDisposition?.FileName == null)
            {
                values[name] = await content.ReadAsStringAsync(cancellationToken);
                continue;
            }

            _ = await content.ReadAsByteArrayAsync(cancellationToken);
            values[name] = string.Empty;
        }

        return values;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler
    )
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
            => handler(request, cancellationToken);
    }
}
