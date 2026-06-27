using System.Net;
using Api.Requests.Abstractions;
using SecShare.Api.Common.Dto.Storage;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Business.Exceptions;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Queue.Handlers;
using SecShare.Business.Services.Storage;

namespace SecShare.Api.Controllers.Storage.Actions;

public class UploadFileRequestHandler : IAsyncRequestHandler<UploadFileRequest, UploadFileResponse>
{
    private const int MaxSourceNameLength = 128;

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
        if (request.Options == null)
        {
            throw new ArgumentException("Upload options are missing.");
        }

        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();

        var fileEntity = await _fileStorage.PutFileAsync(
            fileData,
            CreateSafeUploadFileName(request.Options.SourceName)
        );

        if (!UploadFileExpiration.TryParse(request.Options.Expires, out var expiresIn))
        {
            throw new ApiException(
                $"Options.Expires: {UploadFileExpiration.ValidationErrorMessage}",
                HttpStatusCode.BadRequest
            );
        }

        fileEntity.DeleteAt = DateTime.UtcNow.Add(expiresIn);

        await _queueService.PushDefaultAsync(
            new DeleteFileQueueContext { FileId = fileEntity.Id },
            processAt: fileEntity.DeleteAt.Value
        );

        return new UploadFileResponse
        {
            Token = fileEntity.Id.ToString()
        };
    }

    private static string CreateSafeUploadFileName(string sourceName)
    {
        var safeNameChars = sourceName
            .Where(character => !IsUnsafeFileNameCharacter(character))
            .ToArray();
        var safeName = new string(safeNameChars).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "upload";
        }

        if (safeName.Length > MaxSourceNameLength)
        {
            safeName = safeName[..MaxSourceNameLength];
        }

        return $"{safeName}.secshare";
    }

    private static bool IsUnsafeFileNameCharacter(char character)
    {
        return char.IsControl(character) || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
    }
}
