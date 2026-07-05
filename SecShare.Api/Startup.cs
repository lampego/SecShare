using Autofac;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using SecShare.Api.Components;
using SecShare.Api.Di.Autofac.Modules;
using SecShare.Business;
using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Helpers;
using SecShare.Business.Mvc.Middleware;
using SecShare.Business.Services.Storage;
using Serilog;

namespace SecShare.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        var assembly = typeof(ApiAssemblyMarker).Assembly;

        services.AddCors(options =>
        {
            options.AddPolicy("Cors", policy => policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(
                    SecShareFileHeaders.ContentType,
                    SecShareFileHeaders.FileId,
                    SecShareFileHeaders.FileExtension,
                    SecShareFileHeaders.FileSize,
                    SecShareFileHeaders.DownloadsRemaining,
                    SecShareFileHeaders.DeleteAt,
                    SecShareFileHeaders.PayloadType
                )
            );
        });

        services.AddAutoMapper(cfg => { }, assembly);
        services.AddHttpContextAccessor();
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = FileStorage.MaxFileSize + TransferLimits.MultipartFormDataOverheadBytes;
        });
        services.AddOptions<UploadRateLimitMiddleware.UploadRateLimitOptions>()
            .Bind(Configuration.GetSection(UploadRateLimitMiddleware.UploadRateLimitOptions.SectionName))
            .Validate(
                options => options.UploadBytesPerSecond > 0,
                "UploadRateLimit:UploadBytesPerSecond must be greater than zero."
            )
            .ValidateOnStart();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
        services.AddControllers()
            .AddApplicationPart(assembly)
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
            .AddNewtonsoftJson();
    }

    public virtual void ConfigureContainer(ContainerBuilder containerBuilder)
    {
        containerBuilder
            .RegisterModule<ApiModule>()
            .RegisterAssemblyModules(typeof(BusinessAssemblyMarker).Assembly);
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSerilogRequestLogging();
        }

        ApplicationHelper.HostingEnvironment = env.EnvironmentName;

        app.UseForwardedHeaders();
        app.UseMiddleware<UploadRateLimitMiddleware>();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("Cors");
        app.UseAntiforgery();
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseMiddleware<CommitPerformerMiddleware>();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(SecShare.Web.App).Assembly);
        });
    }
}
