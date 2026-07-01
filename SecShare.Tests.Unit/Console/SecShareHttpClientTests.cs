using System.Net;
using System.Text;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Exceptions;
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
                request.RequestUri
            );
            var multipartContent = Assert.IsType<MultipartFormDataContent>(request.Content);

            var formValues = await ReadMultipartFormValuesAsync(multipartContent, cancellationToken);
            Assert.Contains("file", formValues.Keys);
            Assert.Equal("24h", formValues["Options.Expires"]);
            Assert.Equal("1", formValues["Options.Downloads"]);
            Assert.Equal("File", formValues["Options.ContentType"]);
            Assert.DoesNotContain("Options.SourceName", formValues.Keys);
            Assert.DoesNotContain("Options.HasPassword", formValues.Keys);
            Assert.DoesNotContain(
                formValues,
                item => item.Key.Contains("Key", StringComparison.OrdinalIgnoreCase)
                    || item.Value.Contains("base64UrlKey", StringComparison.Ordinal)
            );

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"token":"file-token"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var result = await client.UploadAsync(
            encryptedPayload,
            new UploadFileOptions
            {
                Expires = "24h",
                Downloads = 1
            },
            progress.Add,
            CancellationToken.None
        );

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
                request.RequestUri
            );

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedPayload),
            }
            );
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var result = await client.DownloadAsync(
            "token",
            progress.Add,
            CancellationToken.None
        );

        Assert.Equal(expectedPayload, result.EncryptedPayload);
        Assert.Equal(StorageContentType.File, result.ContentType);
        Assert.NotEmpty(progress);
        Assert.Equal(expectedPayload.LongLength, progress[^1].BytesTransferred);
        Assert.Equal(expectedPayload.LongLength, progress[^1].TotalBytes);
        Assert.True(progress[^1].BytesPerSecond >= 0);
    }

    [Fact]
    public async Task DownloadAsync_ReadsMetadataHeaders()
    {
        var expectedDeleteAt = DateTime.UtcNow;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("payload"u8.ToArray()),
            };
            response.Headers.Add(SecShareFileHeaders.ContentType, StorageContentType.Text.ToString());
            response.Headers.Add(SecShareFileHeaders.FileId, "file-id");
            response.Headers.Add(SecShareFileHeaders.FileExtension, "secshare");
            response.Headers.Add(SecShareFileHeaders.FileSize, "123");
            response.Headers.Add(SecShareFileHeaders.DownloadsRemaining, "4");
            response.Headers.Add(SecShareFileHeaders.DeleteAt, expectedDeleteAt.ToString("O"));
            response.Headers.Add(SecShareFileHeaders.PayloadType, SecShareFileHeaders.EncryptedArchivePayloadType);

            return Task.FromResult(response);
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var result = await client.DownloadAsync(
            "token",
            progress: null,
            CancellationToken.None
        );

        Assert.Equal(StorageContentType.Text, result.ContentType);
        Assert.Equal("file-id", result.FileId);
        Assert.Equal("secshare", result.Extension);
        Assert.Equal(123, result.Size);
        Assert.Equal(4, result.DownloadsRemaining);
        Assert.Equal(expectedDeleteAt, result.DeleteAt);
        Assert.Equal(SecShareFileHeaders.EncryptedArchivePayloadType, result.PayloadType);
    }

    [Fact]
    public async Task DownloadAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
        );
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.DownloadAsync(
                "missing",
                progress: null,
                CancellationToken.None
            )
        );
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
                    "application/json"
                ),
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(
                "payload"u8.ToArray(),
                new UploadFileOptions
                {
                    Expires = "24h",
                    Downloads = 1
                },
                progress: null,
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task UploadAsync_WithInvalidOptions_ThrowsArgumentExceptionWithFieldNames()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Request should not be sent when options are invalid.")
        );
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => client.UploadAsync(
                "payload"u8.ToArray(),
                new UploadFileOptions
                {
                    Expires = "1w",
                    Downloads = 0
                },
                progress: null,
                CancellationToken.None
            )
        );

        Assert.Contains("Expires: Expires must use a positive duration from 1 second to 365 days with suffix s, m, h, or d.", exception.Message);
        Assert.Contains("Downloads: Downloads must be greater than zero.", exception.Message);
        Assert.Contains(Environment.NewLine, exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WhenApiReturnsValidationErrors_ThrowsApiExceptionWithValidationMessage()
    {
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            _ = await request.Content!.ReadAsByteArrayAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"Options.Expires":["Expires must use a positive duration from 1 second to 365 days with suffix s, m, h, or d."]}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.UploadAsync(
                "payload"u8.ToArray(),
                new UploadFileOptions
                {
                    Expires = "24h",
                    Downloads = 1
                },
                progress: null,
                CancellationToken.None
            )
        );

        Assert.Contains("Options.Expires: Expires must use a positive duration from 1 second to 365 days with suffix s, m, h, or d.", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_SetsConsoleClientTypeHeader()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.True(request.Headers.TryGetValues(SecShareClientHeaders.ClientType, out var values));
            Assert.Equal(SecShareClientHeaders.ClientTypeConsole, values.Single());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("payload"u8.ToArray()),
            }
            );
        });
        using var httpClient = CreateHttpClient(handler);
        var client = new SecShareHttpClient(httpClient);

        _ = await client.DownloadAsync("token", progress: null, CancellationToken.None);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        => new(handler)
        {
            BaseAddress = SecShareConstants.ServiceBaseUri,
        };

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
