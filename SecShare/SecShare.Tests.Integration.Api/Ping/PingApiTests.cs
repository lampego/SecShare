using System.Net;
using System.Net.Http.Json;
using SecShare.Api.Controllers.Ping.Actions;
using SecShare.Tests.Integration.Api.Core;

namespace SecShare.Tests.Integration.Api.Ping;

public class PingApiTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public PingApiTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_Returns_Ok_Status()
    {
        var response = await _client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_Returns_Response_Body()
    {
        var response = await _client.GetFromJsonAsync<PingResponse>("/api/ping");

        Assert.NotNull(response);
        Assert.Equal("ok", response.Status);
    }
}
