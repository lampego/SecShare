using Domain.Abstractions;
using SecShare.Business.Orm.Constants;

namespace SecShare.Business.Services.Queue;

public interface IQueueService : IDomainService
{
    Task PushDefaultAsync(IQueueItemContext itemContext, DateTime? processAt = null);

    Task<int> ProcessAsync(
        QueueChannel channel,
        CancellationToken cancellationToken = default,
        bool isClearSessionForEachIteration = true
    );
}
