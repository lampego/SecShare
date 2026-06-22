using System.Threading;
using System.Threading.Tasks;

namespace Domain.Abstractions;

public interface IAsyncQueueHandler<in TContext> where TContext : IQueueItemContext
{
    Task HandleAsync(TContext commandContext, CancellationToken cancellationToken = default);
}
