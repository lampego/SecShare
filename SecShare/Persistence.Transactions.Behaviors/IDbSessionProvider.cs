using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;

namespace Persistence.Transactions.Behaviors
{
    public interface IDbSessionProvider: IDisposable, IExpectCommit
    {
        ISessionFactory SessionFactory { get; }
        
        ISession CurrentSession { get; }

        ISession CreateSession(FlushMode? flushMode = null);
        
        void CloseCurrentSession();

        Task RollbackCommitAsync(CancellationToken cancellationToken = default);
        
        #region Transaction Management
        
        void SetTransactional(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        Task UnsetTransactional();
        
        ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        
        #endregion
    }
}
