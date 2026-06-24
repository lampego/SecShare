using Api.Requests.Abstractions;
using SecShare.Business.Services.Storage;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Queue.Handlers;
using System.Text.Json;

namespace SecShare.Api.Controllers.Storage.Actions;

public class UploadFileRequestHandler : IAsyncRequestHandler<UploadFileRequest, UploadFileResponse>
{
    private readonly IFileStorage _fileStorage;
    private readonly IQueueService _queueService;

    public UploadFileRequestHandler(IFileStorage fileStorage, IQueueService queueService)
    {
        _fileStorage = fileStorage;
        _queueService = queueService;
    }

    public async Task<UploadFileResponse> ExecuteAsync(UploadFileRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new ArgumentException("Uploaded file is empty or missing.");
        }

        // Validate metadata if provided
        if (!string.IsNullOrWhiteSpace(request.Metadata))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.Metadata);
                // Optionally validate structure or log metadata details
            }
            catch (JsonException)
            {
                throw new ArgumentException("Invalid metadata JSON format.");
            }
        }

        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();

        var fileEntity = await _fileStorage.PutFileAsync(
            fileData,
            request.File.FileName
        );

        if (request.DeleteDelayInSeconds.HasValue && request.DeleteDelayInSeconds.Value > 0)
        {
            fileEntity.DeleteAt = DateTime.UtcNow.AddSeconds(request.DeleteDelayInSeconds.Value);

            await _queueService.PushDefaultAsync(
                new DeleteFileQueueContext { FileId = fileEntity.Id },
                processAt: fileEntity.DeleteAt.Value
            );
        }

        return new UploadFileResponse
        {
            Token = fileEntity.Id.ToString()
        };
    }
}
