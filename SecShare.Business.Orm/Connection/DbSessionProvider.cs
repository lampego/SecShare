using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NHibernate;
using Persistence.Transactions.Behaviors;
using SecShare.Business.Orm.Connection.Interceptors;

namespace SecShare.Business.Orm.Connection;

public class DbSessionProvider : IDbSessionProvider
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<object> _logger;
    private readonly bool _isShowSql;
    private IsolationLevel? _transactionalModeIsolationLevel;
    private ISessionFactory? _sessionFactory;
    private ISession? _session;
    private ITransaction? _transaction;

    public DbSessionProvider(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<object> logger,
        IConfiguration configuration
    )
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _isShowSql = configuration.GetValue("Hibernate:IsShowSql", false);
    }

    public ISessionFactory SessionFactory
    {
        get
        {
            _sessionFactory ??= _dbConnectionFactory.GetSessionFactoryAsync().Result;
            return _sessionFactory;
        }
    }

    public ISession CurrentSession
    {
        get
        {
            OpenCurrentSession();
            return _session!;
        }
    }

    public void SetTransactional(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _transactionalModeIsolationLevel = isolationLevel;
    }

    public async Task UnsetTransactional()
    {
        if (_transactionalModeIsolationLevel == null)
        {
            return;
        }

        if (_transaction is { IsActive: true })
        {
            await _transaction.CommitAsync();
        }

        _transactionalModeIsolationLevel = null;
        _transaction?.Dispose();
        _transaction = null;
    }

    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        OpenCurrentSession();
        return CurrentSession.BeginTransaction(isolationLevel);
    }

    public async Task PerformCommitAsync(bool isCloseConnection = true, CancellationToken cancellationToken = default)
    {
        if (_session is not { IsOpen: true })
        {
            return;
        }

        if (_transaction is { IsActive: true })
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await _session.FlushAsync(cancellationToken);
        }

        if (isCloseConnection)
        {
            CloseCurrentSession();
        }
    }

    public Task RollbackCommitAsync(CancellationToken cancellationToken = default)
    {
        CloseCurrentSession();
        return Task.CompletedTask;
    }

    public void OpenCurrentSession()
    {
        if (_session is not { IsOpen: true })
        {
            _session = CreateSession();
        }

        if (_transactionalModeIsolationLevel != null && _transaction is not { IsActive: true })
        {
            _transaction?.Dispose();
            _transaction = _session.BeginTransaction(_transactionalModeIsolationLevel.Value);
        }
    }

    public ISession CreateSession(FlushMode? flushMode = null)
    {
        if (!_isShowSql)
        {
            return SessionFactory.OpenSession();
        }

        var sessionBuilder = SessionFactory
            .WithOptions()
            .Interceptor(new SqlQueryInterceptor(_logger));

        if (flushMode != null)
        {
            sessionBuilder = sessionBuilder.FlushMode(flushMode.Value);
        }

        return sessionBuilder.OpenSession();
    }

    public void CloseCurrentSession()
    {
        if (_session is not { IsOpen: true })
        {
            return;
        }

        _transaction?.Dispose();
        _transaction = null;
        _session.Close();
        _session = null;
        _transactionalModeIsolationLevel = null;
    }

    public void Dispose()
    {
        CloseCurrentSession();
        GC.SuppressFinalize(this);
    }
}
