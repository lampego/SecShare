using SecShare.Business.Orm.Enums;
using SecShare.Business.Orm.Core;

namespace SecShare.Business.Orm.Entities;

public class QueueEntity : AEntity
{   
    public virtual QueueStatus Status { get; set; }
    public virtual QueueChannel Channel { get; set; }
    public virtual QueuePriority Priority { get; set; }
    public virtual string? Error { get; set; }
    public virtual required string ContextType { get; set; }
    public virtual required string ContextData { get; set; }
    public virtual DateTime ProcessAt { get; set; }
}
