using Domain.Abstractions;
using Microsoft.Extensions.Logging;
using SecShare.Business.Services.Storage;

namespace SecShare.Business.Services.Queue.Handlers;

public class DeleteFileQueueHandler : IAsyncQueueHandler<DeleteFileQueueContext>
{
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DeleteFileQueueHandler> _logger;

    public DeleteFileQueueHandler(IFileStorage fileStorage, ILogger<DeleteFileQueueHandler> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteFileQueueContext commandContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Automatic file deletion triggered for file: {FileId}", commandContext.FileId);
        await _fileStorage.DeleteFileAsync(commandContext.FileId, cancellationToken);
    }
}
