using Autofac;
using Domain.Abstractions;

namespace SecShare.Business.Di.Autofac.Modules;

public class QueueModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(BusinessAssemblyMarker).Assembly)
            .AsClosedTypesOf(typeof(IAsyncQueueHandler<>))
            .InstancePerDependency();
    }
}
