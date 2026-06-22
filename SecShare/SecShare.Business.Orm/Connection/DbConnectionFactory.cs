using Autofac;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.Configuration;
using NHibernate;
using Persistence.Transactions.Behaviors;
using SecShare.Business.Orm.Core.Conventions;
using SecShare.Business.Orm.Core.Dialects;

namespace SecShare.Business.Orm.Connection;

public class DbConnectionFactory : IDbConnectionFactory
{
    private const string DbNamespace = "SecShare.Business.Orm";

    private readonly string _connectionString;
    private ISessionFactory? _sessionFactory;

    public DbConnectionFactory(IConfiguration configuration, ILifetimeScope scope)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured");
    }

    public Task<ISessionFactory> GetSessionFactoryAsync()
    {
        _sessionFactory ??= BuildFactory();
        return Task.FromResult(_sessionFactory);
    }

    public void Dispose()
    {
        _sessionFactory?.Dispose();
        GC.SuppressFinalize(this);
    }

    private ISessionFactory BuildFactory()
    {
        return Fluently.Configure()
            .Database(PostgreSQLConfiguration.PostgreSQL83
                .Dialect<CustomPostgresSqlDialect>()
                .ConnectionString(_connectionString))
            .Mappings(m => m.FluentMappings
                .AddFromAssemblyOf<BusinessOrmAssemblyMarker>()
                .Conventions.Add<SnakeCaseConvention>())
            .BuildSessionFactory();
    }
}
