using Autofac;

namespace SecShare.Business.Testing.Extensions;

public static class ContainerBuilderExtensions
{
    public static void ConfigureTestingScope(this ContainerBuilder builder)
    {
        builder.RegisterAssemblyModules(
            typeof(SecShare.Business.BusinessAssemblyMarker).Assembly,
            typeof(BusinessTestingAssemblyMarker).Assembly
        );
    }
}
