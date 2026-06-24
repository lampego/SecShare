using Persistence.Transactions.Behaviors;

namespace SecShare.Business.Testing.Services;

public class DbCleanUpService : IDbCleanUpService
{
    private readonly IDbSessionProvider _sessionProvider;

    public DbCleanUpService(IDbSessionProvider sessionProvider)
    {
        _sessionProvider = sessionProvider;
    }

    public async Task CleanUp()
    {
        await _sessionProvider.CurrentSession
            .CreateSQLQuery("delete from queues where 1=1;")
            .ExecuteUpdateAsync();
        await _sessionProvider.CurrentSession
            .CreateSQLQuery("delete from files where 1=1;")
            .ExecuteUpdateAsync();
        _sessionProvider.CurrentSession.Clear();
    }
}
