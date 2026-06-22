using Autofac;
using Domain.Abstractions;
using SecShare.Business.Orm;

namespace SecShare.Business.Di.Autofac.Modules;

public class OrmModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(BusinessOrmAssemblyMarker).Assembly)
            .AssignableTo<IDomainService>()
            .AsImplementedInterfaces()
            .InstancePerDependency();
    }
}
