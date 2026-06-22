using Autofac;
using SecShare.Business.Services.Storage.Client;

namespace SecShare.Business.Di.Autofac.Modules;

public class StorageModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<LocalFileStorageClient>()
            .As<IFileStorageClient>()
            .InstancePerLifetimeScope();
    }
}
