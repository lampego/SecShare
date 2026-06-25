using System.Data;
using System.Data.Common;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace SecShare.Business.Orm.Hibernate.DataTypes;

public class TimeOnlyCustomType : IUserType
{
    public SqlType[] SqlTypes => new[] { SqlTypeFactory.Time };
    public Type ReturnedType => typeof(TimeOnly);
    public bool IsMutable => false;

    public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
    {
        var parameter = (IDataParameter)cmd.Parameters[index];
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        if (value is TimeSpan ts)
        {
            parameter.Value = ts;
        }
        else if (value is DateTime dt)
        {
            parameter.Value = dt.TimeOfDay;
        }
        else if (value is TimeOnly to)
        {
            parameter.Value = to.ToTimeSpan();
        }
        else
        {
            throw new InvalidCastException($"Unexpected type {value.GetType()} for TimeOnlyUserType");
        }
    }

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var obj = rs[names[0]];
        if (obj == DBNull.Value) return null!;
        if (obj is TimeSpan ts) return ts;
        if (obj is DateTime dt) return dt.TimeOfDay;
        if (obj is TimeOnly to) return to.ToTimeSpan();
        throw new InvalidCastException($"Unexpected type {obj.GetType()} for TimeOnlyUserType");
    }

    public object DeepCopy(object value) => value;
    public object Replace(object original, object target, object owner) => original;
    public object Assemble(object cached, object owner) => cached;
    public object Disassemble(object value) => value;
    public new bool Equals(object x, object y) => object.Equals(x, y);
    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;
}
