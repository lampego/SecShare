using Majorsoft.Blazor.WebAssembly.Logging.Console;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using SecShare.Business.Common.Http;
using SecShare.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Init Environment config file 
using var webHttp = new HttpClient()
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
};
var configurationFile = "Debug";
#if IS_RELEASE_BUILD
    configurationFile = "Production";
#elif IS_DEBUG_BUILD
    configurationFile = "Debug";
#endif
Console.WriteLine($"Application loaded with {configurationFile} configuration");
using var response = await webHttp.GetAsync($"appsettings.{configurationFile}.json");
using var stream = await response.Content.ReadAsStreamAsync();
builder.Configuration.AddJsonStream(stream);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient using the URL specified in the configurations
var apiUrl = builder.Configuration.GetValue<string>("ApiUrl") ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiUrl)
});
builder.Services.AddScoped<WebSecShareHttpClient>(sp =>
    new WebSecShareHttpClient(sp.GetRequiredService<HttpClient>()));
builder.Services.AddScoped<ISecShareDownloadClient>(sp =>
    sp.GetRequiredService<WebSecShareHttpClient>());

#if DEBUG
builder.Logging.AddBrowserConsole()
    .SetMinimumLevel(LogLevel.Debug)
    .AddFilter("Microsoft", LogLevel.Information);
#endif

await builder.Build().RunAsync();
