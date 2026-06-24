using FluentNHibernate.Mapping;
using NHibernate.Type;
using SecShare.Business.Orm.Hibernate.DataTypes;

namespace SecShare.Business.Orm.Extensions;

public static class FluentExtensions
{
    public static PropertyPart Guid(this PropertyPart part)
    {
        return part.CustomType<Guid>()
            .Not.Nullable();
    }
    
    public static PropertyPart GuidNullable(this PropertyPart part)
    {
        return part.CustomType<Guid>()
            .Nullable();
    }
    
    public static PropertyPart StripeId(this PropertyPart part)
    {
        return part.CustomType<string>()
            .Length(255)
            .Not.Nullable();
    }
    
    public static PropertyPart StripeIdNullable(this PropertyPart part)
    {
        return part.CustomType<string>()
            .Length(255)
            .Nullable();
    }
    
    public static PropertyPart Bool(this PropertyPart part)
    {
        return part.CustomType<bool>()
            .Not.Nullable();
    }
    
    public static PropertyPart BoolNullable(this PropertyPart part)
    {
        return part.CustomType<bool>()
            .Nullable();
    }
    
    public static PropertyPart DateTime(this PropertyPart part)
    {
        return part.CustomType<UtcDateTimeType>()
            .Not.Nullable();
    }
    
    public static PropertyPart DateTimeNullable(this PropertyPart part)
    {
        return part.CustomType<UtcDateTimeType>()
            .Nullable();
    }
    
    public static PropertyPart DateOnly(this PropertyPart part)
    {
        return part.CustomType<DateOnlyCustomType>()
            .CustomSqlType("date")
            .Not.Nullable();
    }
    
    public static PropertyPart DateOnlyNullable(this PropertyPart part)
    {
        return part.CustomType<DateOnlyCustomType>()
            .CustomSqlType("date")
            .Nullable();
    }
    
    public static PropertyPart TimeOnly(this PropertyPart part)
    {
        return part.CustomType<TimeOnlyCustomType>()
            .CustomSqlType("time")
            .Not.Nullable();
    }
    
    public static PropertyPart TimeOnlyNullable(this PropertyPart part)
    {
        return part.CustomType<TimeOnlyCustomType>()
            .CustomSqlType("time")
            .Nullable();
    }
    
    public static PropertyPart Enum(this PropertyPart part)
    {
        return part.Not.Nullable();
    }
    
    public static PropertyPart Enum<T>(this PropertyPart part)
    {
        return part
            .CustomType<T>()
            .CustomSqlType("smallint");
    }
    
    public static PropertyPart EnumNullable<T>(this PropertyPart part)
    {
        return part
            .CustomType<T?>()
            .CustomSqlType("smallint")
            .Nullable();
    }
    
    public static PropertyPart Decimal(this PropertyPart part, int precision = 18, int scale = 4)
    {
        return part.Not.Nullable()
            .Precision(precision)
            .Scale(scale);
    }
    
    public static PropertyPart DecimalNullable(this PropertyPart part, int precision = 18, int scale = 4)
    {
        return part.Nullable()
            .Precision(precision)
            .Scale(scale);
    }
    
    public static PropertyPart ByteArray(this PropertyPart part)
    {
        return part.CustomSqlType("bytea")
            .Length(1024 * 1024 * 100)
            .Not.Nullable();
    }
    
    public static PropertyPart ByteArrayNullable(this PropertyPart part)
    {
        return part.ByteArray()
            .Nullable();
    }
}
