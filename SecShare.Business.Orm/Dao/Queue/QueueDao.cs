using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Transform;
using Persistence.Transactions.Behaviors;
using SecShare.Business.Orm.Enums;
using SecShare.Business.Orm.Entities;
using System.Text.Json;

namespace SecShare.Business.Orm.Dao.Queue;

public class QueueDao : IQueueDao
{
    private readonly ILogger<IQueueDao> _logger;
    private readonly ISession _session;

    public QueueDao(IDbSessionProvider sessionProvider, ILogger<IQueueDao> logger)
    {
        _logger = logger;
        _session = sessionProvider.CreateSession(FlushMode.Manual);
    }

    public async Task Push(
        object context,
        QueueChannel channel = QueueChannel.Default,
        DateTime? processAt = null,
        QueuePriority? priority = null,
        CancellationToken cancellationToken = default
    )
    {
        using var tx = _session.BeginTransaction();
        try
        {
            var contextType = context.GetType();
            var typeString = string.Join(".", contextType.Namespace, contextType.Name);
            var queueItem = new QueueEntity
            {
                Channel = channel,
                Status = QueueStatus.Pending,
                Priority = priority ?? QueuePriority.Normal,
                ContextType = typeString,
                ContextData = JsonSerializer.Serialize(context),
                ProcessAt = processAt ?? DateTime.UtcNow.AddMinutes(-1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _session.SaveAsync(queueItem, cancellationToken);
            await _session.FlushAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await tx.RollbackAsync(cancellationToken);
            Console.WriteLine($"ERROR IN QueueDao.Push: {e}");
            _logger.LogError(e, "{Message}", e.Message);
            throw;
        }
    }
    
    public async Task<QueueEntity?> GetById(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var query = _session.Query<QueueEntity>()
            .Where(item => item.Id == id);
        return await query
            .OrderBy(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<QueueEntity?> GetTop(
        QueueChannel? channel = null,
        CancellationToken cancellationToken = default
    )
    {
        using var tx = _session.BeginTransaction();
        try
        {
            var result = await _session.CreateSQLQuery(@"SELECT * FROM fn_queue_get_top(:channel)")
                .AddEntity(typeof(QueueEntity))
                .SetParameter("channel", (short)(channel ?? QueueChannel.Default))
                .SetResultTransformer(new RootEntityResultTransformer())
                .ListAsync<QueueEntity>(cancellationToken);
            var entity = result?.FirstOrDefault();
            if (entity != null)
                await _session.RefreshAsync(entity, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return entity;
        }
        catch (Exception e)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(e, "{Message}", e.Message);
        }
        return null;
    }

    public async Task MarkAsProcessed(
        QueueEntity item,
        string? error = null,
        CancellationToken cancellationToken = default
    )
    {
        using var tx = _session.BeginTransaction();
        try
        {
            if (item.Status != QueueStatus.InProcess)
            {
                throw new Exception("This item already processed");
            }

            if (string.IsNullOrEmpty(error))
            {
                item.Status = QueueStatus.Success;
            }
            else
            {
                item.Error = error;
                item.Status = QueueStatus.Fail;
            }
            item.UpdatedAt = DateTime.UtcNow;
            await _session.SaveAsync(item, cancellationToken);
            await _session.FlushAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(e, "Error occurred in MarkAsProcessed for queue item {Id}", item.Id);
            throw;
        }
    }

    public async Task<int> CompleteAllPending(CancellationToken cancellationToken = default)
    {
        using var tx = _session.BeginTransaction();
        try
        {
            var count = await _session.Query<QueueEntity>()
                .UpdateBuilder()
                .Set(x => x.Status, QueueStatus.Success)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .UpdateAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return count;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task UpdateProcessAtForPending()
    {
        using var tx = _session.BeginTransaction();
        try
        {
            await _session.Query<QueueEntity>()
                .Where(x => x.Status == QueueStatus.Pending)
                .UpdateBuilder()
                .Set(x => x.ProcessAt, DateTime.UtcNow.AddSeconds(-1))
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .UpdateAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
    
    public void Clear()
    {
        _session.Clear();
    }
    
    public void Dispose()
    {
        Flush().Wait();
        if (_session.IsOpen)
        {
            _session.Dispose();
        }
    }
    
    public async Task Flush()
    {
        await _session.FlushAsync();
    }
}
