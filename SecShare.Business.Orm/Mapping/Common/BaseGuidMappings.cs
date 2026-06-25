using Domain.Abstractions;
using FluentNHibernate.Mapping;
using SecShare.Business.Orm.Core.Generators;

namespace SecShare.Business.Orm.Mapping.Common;

public abstract class BaseGuidMappings<T> : ClassMap<T> where T : IEntity
{
    public BaseGuidMappings()
    {
        Id(x => x.Id)
            .GeneratedBy.Custom<GuidV7Generator>()
            .Unique()
            .Not.Nullable()
            .CustomSqlType("uuid");
    }
}
