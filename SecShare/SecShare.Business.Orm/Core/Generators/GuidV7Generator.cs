using NHibernate.Engine;
using NHibernate.Id;

namespace SecShare.Business.Orm.Core.Generators;

public class GuidV7Generator : IIdentifierGenerator
{
    public Task<object> GenerateAsync(ISessionImplementor session, object obj, CancellationToken cancellationToken)
    {
        return Task.FromResult<object>(Guid.CreateVersion7());
    }

    public object Generate(ISessionImplementor session, object obj)
    {
        return Guid.CreateVersion7();
    }
}
