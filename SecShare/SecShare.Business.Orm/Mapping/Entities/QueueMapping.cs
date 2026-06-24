using SecShare.Business.Orm.Constants;
using SecShare.Business.Orm.Entities;
using SecShare.Business.Orm.Extensions;
using SecShare.Business.Orm.Mapping.Common;

namespace SecShare.Business.Orm.Mapping.Entities;

public class QueueMapping : BaseGuidMappings<QueueEntity>
{
    public QueueMapping()
    {
        Table("queues");
        
        Map(x => x.Status).Enum<QueueStatus>();
        Map(x => x.Channel).Enum<QueueChannel>();
        Map(x => x.Priority).Enum<QueuePriority>();
        Map(x => x.Error).Nullable();
        Map(x => x.ContextType).Not.Nullable();
        Map(x => x.ContextData).Not.Nullable().Length(10485760); // 10MB text data
        
        Map(x => x.ProcessAt).DateTime();
        Map(x => x.CreatedAt).DateTime();
        Map(x => x.UpdatedAt).DateTimeNullable();
        Map(x => x.DeletedAt).DateTimeNullable();
    }
}
