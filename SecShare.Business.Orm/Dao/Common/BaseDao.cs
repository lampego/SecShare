using Autofac;
using NHibernate;
using Persistence.Transactions.Behaviors;

namespace SecShare.Business.Orm.Dao.Common;

public abstract class BaseDao
{
    private readonly IDbSessionProvider _dbSessionProvider;

    protected ISession Session => _dbSessionProvider.CurrentSession;

    protected BaseDao(ILifetimeScope scope)
    {
        _dbSessionProvider = scope.Resolve<IDbSessionProvider>();
    }
}
