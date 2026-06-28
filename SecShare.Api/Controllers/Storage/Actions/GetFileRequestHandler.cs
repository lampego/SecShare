using System.Net;
using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using SecShare.Business.Common.Headers;
using SecShare.Business.Exceptions;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Business.Orm.Dao.Files;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Queue.Handlers;
using SecShare.Business.Services.Storage;
using System.Globalization;

namespace SecShare.Api.Controllers.Storage.Actions;

public class GetFileRequestHandler : IAsyncRequestHandler<GetFileRequest, FileResponse>
{
    private readonly IFilesDao _filesDao;
    private readonly IFileStorage _fileStorage;
    private readonly IQueueService _queueService;

    public GetFileRequestHandler(
        IFilesDao filesDao,
        IFileStorage fileStorage,
        IQueueService queueService
    )
    {
        _filesDao = filesDao;
        _fileStorage = fileStorage;
        _queueService = queueService;
    }

    public async Task<FileResponse> ExecuteAsync(GetFileRequest request)
    {
        if (!Guid.TryParse(request.Id, out var fileId))
        {
            throw new ArgumentException("Invalid file identifier format.");
        }

        var (fileEntity, fileStream) = await _fileStorage.GetFileStreamAsync(fileId);
        var downloadsRemaining = await _filesDao.ConsumeDownloadAsync(fileId);
        if (downloadsRemaining == null)
        {
            await fileStream.DisposeAsync();
            throw new DownloadLimitExhaustedDomainException("Decrypted data is unavailable");
        }

        if (downloadsRemaining == 0)
        {
            await _queueService.PushDefaultAsync(new DeleteFileQueueContext { FileId = fileEntity.Id });
        }

        var response = new FileResponse(fileStream, fileEntity.MimeType)
        {
            FileDownloadName = fileEntity.OriginalFileName
        };
        response.Headers[SecShareFileHeaders.ContentType] = fileEntity.ContentType.ToString();
        response.Headers[SecShareFileHeaders.FileId] = fileEntity.Id.ToString();
        response.Headers[SecShareFileHeaders.FileSize] = fileEntity.Size.ToString(CultureInfo.InvariantCulture);
        response.Headers[SecShareFileHeaders.DownloadsRemaining] = downloadsRemaining.Value.ToString(CultureInfo.InvariantCulture);
        response.Headers[SecShareFileHeaders.PayloadType] = SecShareFileHeaders.EncryptedArchivePayloadType;

        if (!string.IsNullOrWhiteSpace(fileEntity.Extension))
        {
            response.Headers[SecShareFileHeaders.FileExtension] = fileEntity.Extension;
        }

        if (fileEntity.DeleteAt.HasValue)
        {
            response.Headers[SecShareFileHeaders.DeleteAt] = fileEntity.DeleteAt.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        return response;
    }
}
