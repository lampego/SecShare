using Autofac;
using Microsoft.AspNetCore.HttpOverrides;
using SecShare.Api.Di.Autofac.Modules;
using SecShare.Business;
using SecShare.Business.Helpers;
using SecShare.Business.Mvc.Middleware;
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
                .AllowAnyMethod());
        });

        services.AddAutoMapper(cfg => { }, assembly);
        services.AddHttpContextAccessor();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
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
        app.UseRouting();
        app.UseCors("Cors");
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseMiddleware<CommitPerformerMiddleware>();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
