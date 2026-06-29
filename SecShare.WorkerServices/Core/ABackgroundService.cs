using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Persistence.Transactions.Behaviors;
using Serilog.Extensions.Autofac.DependencyInjection;
using SecShare.Business;
using SecShare.Business.Helpers;
using SecShare.Business.Logging;
using SecShare.Business.Orm.Dao.Queue;

namespace SecShare.WorkerServices.Core;

public abstract class ABackgroundService : BackgroundService
{
    protected readonly ILogger<ABackgroundService> _logger;
    private readonly CrontabSchedule _crontabScheduler;

    protected string ServiceName = "BackgroundService";
    protected readonly IQueueDao QueueDao;
    
    private DateTime _nextTickTime;
    protected ILifetimeScope DiScope { get; set; }
    protected readonly IDbSessionProvider DbSessionProvider;
    private CancellationToken _cancellationToken;

    private bool _isShouldRunWork
    {
        get => DateTime.UtcNow > _nextTickTime;
    }

    protected virtual bool IsEnableLogging { get; set; } = true;
    
    public ABackgroundService()
    {   
        var builder = new ContainerBuilder();
        builder.RegisterAssemblyModules(
            typeof(BusinessAssemblyMarker).Assembly
        );
        
        var configuration = ApplicationHelper.BuildConfiguration();
        builder.RegisterInstance(configuration)
            .As<IConfiguration>()
            .SingleInstance();
        
        // Logger
        var serilogConfiguration = LoggerInitializer.GetSerilogBuilder(false);        
        builder.RegisterSerilog(serilogConfiguration);
        
        var diContainer = builder.Build();
        DiScope = diContainer.BeginLifetimeScope();
        
        _logger = DiScope.Resolve<ILogger<ABackgroundService>>();
        QueueDao = DiScope.Resolve<IQueueDao>();
        try
        {
            DbSessionProvider = DiScope.Resolve<IDbSessionProvider>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to resolve IDbSessionProvider");
            throw;
        }
        
        _crontabScheduler = CrontabSchedule.Parse(
            GetCrontabExpression()
        );
        UpdateNextTickTime();
        ServiceName = this.GetType().Name;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = stoppingToken;
        Log("Processing Hosted Service is starting.");
        
        stoppingToken.Register(() =>
        {
            Log($"Processing Hosted Service is stopping because cancelled.");
        }
        );

        await Task.Run(async () =>
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_isShouldRunWork)
                {
                    var startTime = DateTime.UtcNow;
                    try
                    {
                        await DoWorkAsync(_cancellationToken);
                        await QueueDao.Flush();
                        await DbSessionProvider.PerformCommitAsync(true, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        QueueDao.Clear();
                        _logger.LogError(e, "{Message}", e.Message);
                    }
                    finally
                    {
                        DbSessionProvider.CloseCurrentSession();
                    }

                    var difference = DateTime.UtcNow - startTime;
                    if (IsEnableLogging)
                    {
                        Log("Duration of work: " + difference.ToString("g"));
                    }
                    UpdateNextTickTime();
                }

                Thread.Sleep(1000);
            }
        },
        _cancellationToken
        );
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Log($"Processing Hosted Service is stopping.");
        await base.StopAsync(stoppingToken);
    }

    private void UpdateNextTickTime()
    {
        _nextTickTime = _crontabScheduler.GetNextOccurrence(DateTime.UtcNow, DateTime.MaxValue);
        Log($"Next work scheduled at: {_nextTickTime}");
    }

    protected void Log(string message)
    {
        _logger.LogInformation("{ServiceName}: {Message}", ServiceName, message);
    }

    public override void Dispose()
    {
        DbSessionProvider.Dispose();
        DiScope.Dispose();
        base.Dispose();
    }
    
    protected virtual string GetCrontabExpression() => "* * * * *";

    protected abstract Task DoWorkAsync(CancellationToken cancellationToken);
}
