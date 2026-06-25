using Api.Requests.Abstractions;
using Autofac;
using Domain.Abstractions;

namespace SecShare.Api.Di.Autofac.Modules;

public class ApiModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<HttpContextAccessor>()
            .As<IHttpContextAccessor>()
            .InstancePerLifetimeScope();

        builder
            .RegisterAssemblyTypes(typeof(ApiAssemblyMarker).Assembly)
            .AsClosedTypesOf(typeof(IAsyncRequestHandler<>))
            .InstancePerDependency();

        builder
            .RegisterAssemblyTypes(typeof(ApiAssemblyMarker).Assembly)
            .AsClosedTypesOf(typeof(IAsyncRequestHandler<,>))
            .InstancePerDependency();

        builder
            .RegisterAssemblyTypes(typeof(ApiAssemblyMarker).Assembly)
            .AssignableTo<IDomainService>()
            .AsImplementedInterfaces()
            .InstancePerDependency();

        builder
            .RegisterType<ScopedAsyncRequestHandlerFactory>()
            .As<IAsyncRequestHandlerFactory>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<DefaultAsyncRequestBuilder>()
            .As<IAsyncRequestBuilder>()
            .InstancePerLifetimeScope();
    }
}
