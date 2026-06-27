using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Business.Orm.Dao.Files;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Queue.Handlers;
using SecShare.Business.Services.Storage;

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
            throw new InvalidOperationException($"File was not found: {fileId}");
        }

        if (downloadsRemaining == 0)
        {
            await _queueService.PushDefaultAsync(new DeleteFileQueueContext { FileId = fileEntity.Id });
        }

        return new FileResponse(fileStream, fileEntity.MimeType)
        {
            FileDownloadName = fileEntity.OriginalFileName
        };
    }
}
