using Autofac;
using Microsoft.Extensions.Configuration;
using SecShare.Api;
using SecShare.Business.Testing.Extensions;

namespace SecShare.Tests.Integration.Api;

public class TestStartup : Startup
{
    public TestStartup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        base.ConfigureContainer(containerBuilder);
        containerBuilder.ConfigureTestingScope();
    }
}
