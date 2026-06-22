using Autofac;
using Domain.Abstractions;
using SecShare.Business.Testing.Factories;

namespace SecShare.Business.Testing.Autofac;

public class TestingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(BusinessTestingAssemblyMarker).Assembly)
            .AsClosedTypesOf(typeof(IDataFactory<>))
            .InstancePerDependency();

        builder
            .RegisterAssemblyTypes(typeof(BusinessTestingAssemblyMarker).Assembly)
            .AssignableTo<IDomainService>()
            .AsImplementedInterfaces()
            .InstancePerDependency();
    }
}
