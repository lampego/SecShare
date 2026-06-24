using System.Net;
using System.Net.Http.Json;
using System.Text;
using SecShare.Api.Controllers.Storage.Actions;
using SecShare.Business.Dto;
using SecShare.Business.Services.Queue;
using SecShare.Business.Orm.Dao.Queue;
using SecShare.Business.Orm.Constants;
using Microsoft.Extensions.DependencyInjection;
using SecShare.Tests.Integration.Api.Core;

namespace SecShare.Tests.Integration.Api.Storage;

public class StorageApiTests : BaseTest
{
    private const string UploadRoute = "/api/file/upload";
    private const string GetRoutePrefix = "/api/file/get/";
    private const string AlternativeUploadRoute = "/api/files";
    private const string AlternativeGetRoutePrefix = "/api/files/";

    public StorageApiTests(ApiCustomWebApplicationFactory factory) : base(factory)
    {
    }

    private void AddConsoleHeaders()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Client-Type");
        HttpClient.DefaultRequestHeaders.Add("X-Client-Type", "Console");
        HttpClient.DefaultRequestHeaders.UserAgent.Clear();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SecShareConsole/1.0");
    }

    private void RemoveConsoleHeaders()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Client-Type");
        HttpClient.DefaultRequestHeaders.UserAgent.Clear();
    }

    [Fact]
    public async Task Upload_WithoutConsoleHeaders_ReturnsForbidden()
    {
        RemoveConsoleHeaders();

        var fileBytes = Encoding.UTF8.GetBytes("Test file contents");
        var formFile = CreateFormFile("archive.secshare", fileBytes);
        var data = new Dictionary<string, object>
        {
            { "metadata", "{\"Expires\":\"24h\",\"Downloads\":1,\"HasPassword\":false,\"SourceName\":\"test\"}" }
        };

        var response = await PostMultipartFormDataRequestAsync(UploadRoute, data, formFile);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync<JsonCommonResponse>();
        Assert.NotNull(errorResponse);
        Assert.Equal("fail", errorResponse.Status);
        Assert.Equal("ApiException", errorResponse.ErrorCode);
        Assert.Equal("Forbidden: Requests are only allowed from the SecShare Console application.", errorResponse.Message);
    }

    [Fact]
    public async Task Upload_WithConsoleHeaders_ReturnsSuccessAndToken()
    {
        AddConsoleHeaders();

        var fileBytes = Encoding.UTF8.GetBytes("Test file contents");
        var formFile = CreateFormFile("archive.secshare", fileBytes);
        var data = new Dictionary<string, object>
        {
            { "metadata", "{\"Expires\":\"24h\",\"Downloads\":1,\"HasPassword\":false,\"SourceName\":\"test\"}" }
        };

        var response = await PostMultipartFormDataRequestAsync(UploadRoute, data, formFile);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var uploadResponse = await response.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(uploadResponse);
        Assert.True(Guid.TryParse(uploadResponse.Token, out _));
    }

    [Fact]
    public async Task Download_WithoutConsoleHeaders_ReturnsForbidden()
    {
        RemoveConsoleHeaders();

        var response = await HttpClient.GetAsync($"{GetRoutePrefix}{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync<JsonCommonResponse>();
        Assert.NotNull(errorResponse);
        Assert.Equal("fail", errorResponse.Status);
        Assert.Equal("ApiException", errorResponse.ErrorCode);
        Assert.Equal("Forbidden: Requests are only allowed from the SecShare Console application.", errorResponse.Message);
    }

    [Fact]
    public async Task Download_WithConsoleHeaders_ReturnsFileStream()
    {
        // 1. Upload file with console headers to get a token
        AddConsoleHeaders();

        var originalContent = "Secure secret data to share";
        var fileBytes = Encoding.UTF8.GetBytes(originalContent);
        var formFile = CreateFormFile("archive.secshare", fileBytes);
        var data = new Dictionary<string, object>
        {
            { "metadata", "{\"Expires\":\"24h\",\"Downloads\":1,\"HasPassword\":false,\"SourceName\":\"test\"}" }
        };

        var uploadResponse = await PostMultipartFormDataRequestAsync(UploadRoute, data, formFile);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploadResponseDto = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(uploadResponseDto);
        var fileToken = uploadResponseDto.Token;

        // 2. Download file using token and verify stream
        var downloadResponse = await HttpClient.GetAsync($"{GetRoutePrefix}{fileToken}");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        var downloadedContent = Encoding.UTF8.GetString(downloadedBytes);

        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task DualRoutes_UploadAndGet_WorkPerfectly()
    {
        AddConsoleHeaders();

        var originalContent = "Dual route validation test contents";
        var fileBytes = Encoding.UTF8.GetBytes(originalContent);
        var formFile = CreateFormFile("archive.secshare", fileBytes);
        var data = new Dictionary<string, object>
        {
            { "metadata", "{\"Expires\":\"24h\",\"Downloads\":1,\"HasPassword\":false,\"SourceName\":\"test\"}" }
        };

        // Test the alternative POST route: /api/files
        var uploadResponse = await PostMultipartFormDataRequestAsync(AlternativeUploadRoute, data, formFile);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploadResponseDto = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(uploadResponseDto);
        var fileToken = uploadResponseDto.Token;

        // Test the alternative GET route: /api/files/{id}
        var downloadResponse = await HttpClient.GetAsync($"{AlternativeGetRoutePrefix}{fileToken}");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        var downloadedContent = Encoding.UTF8.GetString(downloadedBytes);

        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task Upload_WithDeleteDelay_AndProcessingQueue_DeletesFile()
    {
        await CleanUpDbAsync();
        AddConsoleHeaders();

        var originalContent = "File to be automatically deleted by background queue";
        var fileBytes = Encoding.UTF8.GetBytes(originalContent);
        var formFile = CreateFormFile("autodelete.txt", fileBytes);
        var data = new Dictionary<string, object>
        {
            { "metadata", "{\"SourceName\":\"test_autodelete\"}" },
            { "deleteDelayInSeconds", 30 }
        };

        // 1. Upload via API
        var uploadResponse = await PostMultipartFormDataRequestAsync(UploadRoute, data, formFile);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploadResponseDto = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(uploadResponseDto);
        var fileToken = uploadResponseDto.Token;

        // Verify we can download it first
        var downloadResponseBefore = await HttpClient.GetAsync($"{GetRoutePrefix}{fileToken}");
        Assert.Equal(HttpStatusCode.OK, downloadResponseBefore.StatusCode);

        // 2. Resolve queue services from test ServiceProvider to fast-forward and process the queue
        var queueService = ServiceProvider.GetRequiredService<IQueueService>();
        var queueDao = ServiceProvider.GetRequiredService<IQueueDao>();

        // Fast-forward processing time
        await queueDao.UpdateProcessAtForPending();
        await queueDao.Flush();
        await FlushDbChanges();

        // 3. Process the queue
        var processedCount = await queueService.ProcessAsync(QueueChannel.Default);
        Assert.Equal(1, processedCount);

        // 4. Verify we cannot download it anymore
        var downloadResponseAfter = await HttpClient.GetAsync($"{GetRoutePrefix}{fileToken}");
        Assert.NotEqual(HttpStatusCode.OK, downloadResponseAfter.StatusCode);
    }
}
