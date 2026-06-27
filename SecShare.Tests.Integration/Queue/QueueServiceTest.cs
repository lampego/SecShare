using Autofac;
using NHibernate.Linq;
using SecShare.Business.Orm.Enums;
using SecShare.Business.Orm.Dao.Queue;
using SecShare.Business.Orm.Entities;
using SecShare.Business.Services.Queue;
using SecShare.Business.Services.Queue.Handlers;
using SecShare.Business.Services.Storage;
using SecShare.Tests.Integration.Core;

namespace SecShare.Tests.Integration.Queue;

public class QueueServiceTest : BaseTest
{
    private readonly IQueueService _queueService;
    private readonly IQueueDao _queueDao;
    private readonly IFileStorage _fileStorage;

    public QueueServiceTest() : base()
    {
        _queueService = Scope.Resolve<IQueueService>();
        _queueDao = Scope.Resolve<IQueueDao>();
        _fileStorage = Scope.Resolve<IFileStorage>();
    }

    [Fact]
    public async Task ShouldPushAndProcessQueueItem()
    {

        var content = "This is a temporary file for automatic deletion"u8.ToArray();
        var fileEntity = await _fileStorage.PutFileAsync(
            content,
            "autodelete.txt"
        );
        await FlushDbChanges();

        var context = new DeleteFileQueueContext { FileId = fileEntity.Id };
        await _queueService.PushDefaultAsync(context);
        await _queueDao.UpdateProcessAtForPending();
        await _queueDao.Flush();
        await FlushDbChanges();

        DbSessionProvider.CurrentSession.Clear();
        var allItemsBefore = await DbSessionProvider.CurrentSession.Query<QueueEntity>().ToListAsync();
        foreach (var item in allItemsBefore)
        {
            Console.WriteLine($"BEFORE PROCESS: ID={item.Id}, Status={item.Status}, ProcessAt={item.ProcessAt:O}, Now={DateTime.UtcNow:O}");
        }

        var processedCount = await _queueService.ProcessAsync(QueueChannel.Default);
        Assert.Equal(1, processedCount);

        DbSessionProvider.CurrentSession.Clear();
        var allItems = await DbSessionProvider.CurrentSession.Query<QueueEntity>().ToListAsync();
        Assert.Single(allItems);
        Assert.Equal(QueueStatus.Success, allItems[0].Status);
    }

    [Fact]
    public async Task ShouldAutoDeleteFileViaQueue()
    {

        var content = "This is a temporary file for automatic deletion"u8.ToArray();
        var fileEntity = await _fileStorage.PutFileAsync(
            content,
            "autodelete.txt"
        );
        fileEntity.DeleteAt = DateTime.UtcNow.AddSeconds(30);
        await _queueService.PushDefaultAsync(
            new DeleteFileQueueContext { FileId = fileEntity.Id },
            processAt: fileEntity.DeleteAt.Value
        );

        await _queueDao.Flush();
        await FlushDbChanges();

        // Verify file exists
        var (file, fileStream) = await _fileStorage.GetFileStreamAsync(fileEntity.Id);
        Assert.NotNull(file);
        Assert.False(file.IsDeleted);
        fileStream.Dispose();

        // Verify queue job is scheduled
        var topPendingBefore = await _queueDao.GetTop(QueueChannel.Default);
        // It shouldn't be picked up yet because process_at is in the future
        Assert.Null(topPendingBefore);

        // Fast-forward pending queue items
        await _queueDao.UpdateProcessAtForPending();
        await _queueDao.Flush();

        // Now process queue item
        var processedCount = await _queueService.ProcessAsync(QueueChannel.Default);
        Assert.Equal(1, processedCount);

        // Verify file is soft-deleted
        var fileAfter = await DbSessionProvider.CurrentSession.GetAsync<FileEntity>(fileEntity.Id);
        Assert.NotNull(fileAfter);
        Assert.True(fileAfter.IsDeleted);
    }
}
