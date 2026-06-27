using System.ComponentModel.DataAnnotations;
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

        ValidateOptions(request.Options);

        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();

        var fileEntity = await _fileStorage.PutFileAsync(
            fileData,
            $"{Guid.CreateVersion7()}.secshare"
        );
        fileEntity.DownloadsRemaining = request.Options.Downloads;

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

    private static void ValidateOptions(UploadFileOptions options)
    {
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true
        );

        if (isValid)
        {
            return;
        }

        var errorMessages = validationResults.SelectMany(FormatValidationResultMessages);
        throw new ApiException(
            string.Join("; ", errorMessages),
            HttpStatusCode.BadRequest
        );
    }

    private static IEnumerable<string> FormatValidationResultMessages(ValidationResult validationResult)
    {
        var errorMessage = validationResult.ErrorMessage ?? "Invalid upload option.";
        var memberNames = validationResult.MemberNames.ToArray();
        if (memberNames.Length == 0)
        {
            yield return $"Options: {errorMessage}";
            yield break;
        }

        foreach (var memberName in memberNames)
        {
            yield return $"Options.{memberName}: {errorMessage}";
        }
    }
}
