using System.Text;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.Transactions.Behaviors;
using SecShare.Business;
using SecShare.Business.Helpers;
using SecShare.Business.Testing;
using SecShare.Business.Testing.Services;


namespace SecShare.Tests.Integration.Core;

public abstract class BaseTest : IDisposable, IAsyncLifetime
{
    protected readonly ILifetimeScope Scope;

    private readonly IContainer _container;
    private IDbSessionProvider? _dbSessionProvider;
    private IDbCleanUpService? _dbCleanUpService;

    protected IDbSessionProvider DbSessionProvider =>
        _dbSessionProvider ??= Scope.Resolve<IDbSessionProvider>();

    protected IDbCleanUpService DbCleanUpService =>
        _dbCleanUpService ??= Scope.Resolve<IDbCleanUpService>();

    protected BaseTest()
    {
        var configuration = ApplicationHelper.BuildConfiguration();

        var builder = new ContainerBuilder();
        builder
            .RegisterInstance(configuration)
            .As<IConfiguration>()
            .SingleInstance();

        builder
            .RegisterGeneric(typeof(NullLogger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();

        builder.RegisterAssemblyModules(
            typeof(BusinessAssemblyMarker).Assembly,
            typeof(BusinessTestingAssemblyMarker).Assembly
        );

        _container = builder.Build();
        Scope = _container.BeginLifetimeScope();
    }

    public async Task InitializeAsync()
    {
        await CleanUpDbAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected Task CleanUpDbAsync()
    {
        return DbCleanUpService.CleanUp();
    }

    protected async Task FlushDbChanges(bool isClearSession = false)
    {
        await DbSessionProvider.CurrentSession.FlushAsync();
        if (isClearSession)
        {
            DbSessionProvider.CurrentSession.Clear();
        }
    }

    protected async Task RefreshEntity(object entity)
    {
        await DbSessionProvider.CurrentSession.RefreshAsync(entity);
    }

    protected async Task FlushAndRefreshEntity(object entity, bool isClearSession = false)
    {
        await FlushDbChanges(isClearSession);
        await DbSessionProvider.CurrentSession.RefreshAsync(entity);
    }

    protected IFormFile CreateFormFile(string fileName = "test.pdf", byte[]? fileBytes = null)
    {
        var stream = new MemoryStream();
        if (fileBytes != null)
        {
            stream.Write(fileBytes);
        }
        else
        {
            var content = "Hello World from a Fake File";
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "id_from_form", fileName);
    }

    public void Dispose()
    {
        Scope.Dispose();
        _container.Dispose();
        GC.SuppressFinalize(this);
    }
}
