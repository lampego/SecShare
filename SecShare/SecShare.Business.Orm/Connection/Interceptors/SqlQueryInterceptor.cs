using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.SqlCommand;

namespace SecShare.Business.Orm.Connection.Interceptors;

public class SqlQueryInterceptor : EmptyInterceptor
{
    private readonly ILogger<object> _logger;

    public SqlQueryInterceptor(ILogger<object> logger)
    {
        _logger = logger;
    }

    public override SqlString OnPrepareStatement(SqlString sql)
    {
        _logger.LogDebug("NHibernate: {Sql}", sql);
        return base.OnPrepareStatement(sql);
    }
}
