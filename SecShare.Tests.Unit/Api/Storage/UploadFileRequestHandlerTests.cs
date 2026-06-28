using System.Net;
using Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Api.Controllers.Storage.Actions;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Business.Exceptions;
using SecShare.Business.Orm.Enums;
using SecShare.Business.Orm.Entities;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Storage;
using StorageContentType = SecShare.Business.Common.Enums.StorageContentType;

namespace SecShare.Tests.Unit.Api.Storage;

public sealed class UploadFileRequestHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidOptions_StoresDownloadLimitAndSchedulesExpiration()
    {
        var fileStorage = new TestFileStorage();
        var queueService = new TestQueueService();
        var handler = new UploadFileRequestHandler(fileStorage, queueService);
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = new UploadFileOptions
            {
                Expires = "24h",
                Downloads = 5,
                ContentType = StorageContentType.Text
            }
        };

        var response = await handler.ExecuteAsync(request);

        Assert.True(Guid.TryParse(response.Token, out _));
        Assert.True(fileStorage.HasPutFileBeenCalled);
        Assert.NotNull(fileStorage.CreatedFile);
        Assert.Equal(request.Options.Downloads, fileStorage.CreatedFile.DownloadsRemaining);
        Assert.Equal(StorageContentType.Text, fileStorage.CreatedFile.ContentType);
        Assert.True(queueService.HasPushDefaultBeenCalled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_WithInvalidOptions_ThrowsBadRequestBeforeStoringFile(
        int downloads
    )
    {
        var fileStorage = new TestFileStorage();
        var queueService = new TestQueueService();
        var handler = new UploadFileRequestHandler(fileStorage, queueService);
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = new UploadFileOptions
            {
                Expires = "24h",
                Downloads = downloads
            }
        };

        var exception = await Assert.ThrowsAsync<ApiException>(() => handler.ExecuteAsync(request));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("Options.Downloads", exception.Message);
        Assert.Contains("Downloads must be greater than zero.", exception.Message);
        Assert.False(fileStorage.HasPutFileBeenCalled);
        Assert.False(queueService.HasPushDefaultBeenCalled);
    }

    private sealed class TestFileStorage : IFileStorage
    {
        public bool HasPutFileBeenCalled { get; private set; }
        public FileEntity? CreatedFile { get; private set; }

        public Task<FileEntity> PutFileAsync(
            byte[] fileData,
            string fileName,
            string? encryptionAlgorithm = null,
            string? encryptionKeyId = null,
            CancellationToken cancellationToken = default
        )
        {
            HasPutFileBeenCalled = true;

            CreatedFile = new FileEntity
            {
                Id = Guid.NewGuid(),
                StoragePath = "files/test.secshare",
                MimeType = "application/octet-stream",
                OriginalFileName = fileName,
                Size = fileData.Length,
                EncryptionAlgorithm = encryptionAlgorithm,
                EncryptionKeyId = encryptionKeyId
            };

            return Task.FromResult(CreatedFile);
        }

        public Task<(FileEntity File, Stream FileStream)> GetFileStreamAsync(
            Guid fileId,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotSupportedException();
        }

        public Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestQueueService : IQueueService
    {
        public bool HasPushDefaultBeenCalled { get; private set; }

        public Task PushDefaultAsync(IQueueItemContext itemContext, DateTime? processAt = null)
        {
            HasPushDefaultBeenCalled = true;

            return Task.CompletedTask;
        }

        public Task<int> ProcessAsync(
            QueueChannel channel,
            CancellationToken cancellationToken = default,
            bool isClearSessionForEachIteration = true
        )
        {
            throw new NotSupportedException();
        }
    }

    private static IFormFile CreateFormFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, nameof(UploadFileRequest.File), "test.txt");
    }
}
