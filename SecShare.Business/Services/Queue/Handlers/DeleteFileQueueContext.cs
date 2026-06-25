using Domain.Abstractions;

namespace SecShare.Business.Services.Queue.Handlers;

public class DeleteFileQueueContext : IQueueItemContext
{
    public Guid FileId { get; set; }
}
