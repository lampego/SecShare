using NHibernate;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;

namespace SecShare.Business.Orm.Core.Dialects;

public class CustomPostgresSqlDialect : PostgreSQL82Dialect
{
    public CustomPostgresSqlDialect()
    {
        RegisterFunction("concat", new VarArgsSQLFunction(NHibernateUtil.String, "", " || ", ""));
        RegisterFunction("coalesce", new VarArgsSQLFunction(NHibernateUtil.String, "coalesce(", ", ", ")"));
        RegisterFunction("lower", new StandardSQLFunction("lower", NHibernateUtil.String));
    }
}
