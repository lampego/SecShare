using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using SecShare.Business.Services.Storage;

namespace SecShare.Api.Controllers.Storage.Actions;

public class GetFileRequestHandler : IAsyncRequestHandler<GetFileRequest, FileResponse>
{
    private readonly IFileStorage _fileStorage;

    public GetFileRequestHandler(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<FileResponse> ExecuteAsync(GetFileRequest request)
    {
        if (!Guid.TryParse(request.Id, out var fileId))
        {
            throw new ArgumentException("Invalid file identifier format.");
        }

        var (fileEntity, fileStream) = await _fileStorage.GetFileStreamAsync(fileId);

        return new FileResponse(fileStream, fileEntity.MimeType)
        {
            FileDownloadName = fileEntity.OriginalFileName
        };
    }
}
