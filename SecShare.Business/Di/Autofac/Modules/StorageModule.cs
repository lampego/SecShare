using Autofac;
using SecShare.Business.Services.Storage.Client;

namespace SecShare.Business.Di.Autofac.Modules;

public class StorageModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<FileStorageS3Client>()
            .As<IFileStorageS3Client>()
            .InstancePerLifetimeScope();

        builder
            .RegisterType<FileStorageGarageClient>()
            .As<IFileStorageGarageClient>()
            .InstancePerLifetimeScope();
    }
}
