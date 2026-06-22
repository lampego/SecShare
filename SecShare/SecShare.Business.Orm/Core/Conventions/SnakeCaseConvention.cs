using System.Text.RegularExpressions;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;

namespace SecShare.Business.Orm.Core.Conventions;

public class SnakeCaseConvention :
    IClassConvention,
    IPropertyConvention,
    IPropertyConventionAcceptance,
    IReferenceConvention,
    IReferenceConventionAcceptance
{
    public void Apply(IClassInstance instance)
    {
        instance.Table(ToSnakeCase(instance.EntityType.Name.Replace("Entity", "")));
    }

    public void Accept(IAcceptanceCriteria<IPropertyInspector> criteria)
    {
        criteria.Expect(x => string.IsNullOrEmpty(x.Formula));
    }

    public void Accept(IAcceptanceCriteria<IManyToOneInspector> criteria)
    {
        criteria.Expect(x => string.IsNullOrEmpty(x.Formula));
    }

    public void Apply(IPropertyInstance instance)
    {
        instance.Column(ToSnakeCase(instance.Name));
    }

    public void Apply(IManyToOneInstance instance)
    {
        var columnName = ToSnakeCase(instance.Name);
        if (!columnName.EndsWith("_id", StringComparison.Ordinal))
        {
            columnName += "_id";
        }

        instance.Column(columnName);
    }

    private static string ToSnakeCase(string name)
    {
        var result = Regex.Replace(name, @"(?<=[a-z0-9])([A-Z])", "_$1");
        result = Regex.Replace(result, @"(?<=[A-Za-z])([0-9])", "_$1");
        result = Regex.Replace(result, @"(?<=[0-9])([A-Za-z])", "_$1");
        return result.ToLowerInvariant();
    }
}
