using System;
using System.Threading.Tasks;
using NHibernate;

namespace Persistence.Transactions.Behaviors
{
    public interface IDbConnectionFactory: IDisposable
    {
        Task<ISessionFactory> GetSessionFactoryAsync();
    }
}