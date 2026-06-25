using System.Data;
using System.Data.Common;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace SecShare.Business.Orm.Hibernate.DataTypes;

public class DateOnlyCustomType : IUserType
{
    public SqlType[] SqlTypes => new[] { SqlTypeFactory.Date };
    public Type ReturnedType => typeof(DateOnly);
    public bool IsMutable => false;

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var obj = rs[names[0]];
        if (obj == DBNull.Value) return null!;
        if (obj is DateTime dt) return DateOnly.FromDateTime(dt);
        if (obj is DateOnly d) return d;
        throw new InvalidCastException($"Unexpected type {obj.GetType()} for DateOnlyUserType");
    }

    public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
    {
        var parameter = (IDataParameter)cmd.Parameters[index];
        if (value == null)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        if (value is DateOnly d)
        {
            parameter.Value = d.ToDateTime(TimeOnly.MinValue);
        }
        else if (value is DateTime dt)
        {
            parameter.Value = dt.Date;
        }
        else
        {
            throw new InvalidCastException($"Unexpected type {value.GetType()} for DateOnlyUserType");
        }
    }

    public object DeepCopy(object value) => value;
    public object Replace(object original, object target, object owner) => original;
    public object Assemble(object cached, object owner) => cached;
    public object Disassemble(object value) => value;
    public new bool Equals(object x, object y) => object.Equals(x, y);
    public int GetHashCode(object x) => x?.GetHashCode() ?? 0;
}
