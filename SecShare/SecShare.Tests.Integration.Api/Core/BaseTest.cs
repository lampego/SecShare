using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Persistence.Transactions.Behaviors;
using SecShare.Business.Testing.Services;
using HttpClient = System.Net.Http.HttpClient;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SecShare.Tests.Integration.Api.Core;

public abstract class BaseTest : IClassFixture<ApiCustomWebApplicationFactory>, IDisposable, IAsyncLifetime
{
    protected readonly ApiCustomWebApplicationFactory Factory;
    protected readonly IServiceScope ServiceScope;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly HttpClient HttpClient;

    private IDbSessionProvider? _dbSessionProvider;
    private IDbCleanUpService? _dbCleanUpService;

    protected IDbSessionProvider DbSessionProvider =>
        _dbSessionProvider ??= ServiceProvider.GetRequiredService<IDbSessionProvider>();

    protected IDbCleanUpService DbCleanUpService =>
        _dbCleanUpService ??= ServiceProvider.GetRequiredService<IDbCleanUpService>();

    protected BaseTest(ApiCustomWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = Factory.CreateClient();
        ServiceScope = Factory.Services.CreateScope();
        ServiceProvider = ServiceScope.ServiceProvider;
    }

    public async Task InitializeAsync()
    {
        await CleanUpDbAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        ServiceScope.Dispose();
        GC.SuppressFinalize(this);
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

    protected async Task<HttpResponseMessage> PostRequestAsAnonymousAsync(string url, object? data = null)
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        var requestData = JsonContent.Create(data ?? new { });
        return await HttpClient.PostAsync(url, requestData);
    }

    protected async Task<HttpResponseMessage> GetRequestAsAnonymousAsync(
        string url,
        Dictionary<string, string>? urlParams = null
    )
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        HttpClient.DefaultRequestHeaders.Remove(HeaderNames.Accept);
        HttpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");

        urlParams ??= new Dictionary<string, string>();
        var queryParams = urlParams.ToDictionary(item => item.Key, item => (string?)item.Value);
        var uri = new Uri(QueryHelpers.AddQueryString(url, queryParams), UriKind.Relative);
        return await HttpClient.GetAsync(uri);
    }

    protected async Task<TResponse?> GetJsonAsAnonymousAsync<TResponse>(
        string url,
        Dictionary<string, string>? urlParams = null
    )
    {
        var response = await GetRequestAsAnonymousAsync(url, urlParams);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    protected async Task<HttpResponseMessage> PostMultipartFormDataRequestAsync(
        string url,
        Dictionary<string, object>? data = null,
        IFormFile? file = null
    )
    {
        using var multipartFormContent = new MultipartFormDataContent();
        if (data != null)
        {
            foreach (var dataKeyPair in data)
            {
                multipartFormContent.Add(new StringContent($"{dataKeyPair.Value}"), name: dataKeyPair.Key);
            }
        }

        if (file != null)
        {
            var fileStreamContent = new StreamContent(file.OpenReadStream());
            multipartFormContent.Add(fileStreamContent, name: "File", fileName: file.FileName);
        }

        return await HttpClient.PostAsync(url, multipartFormContent);
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
}
