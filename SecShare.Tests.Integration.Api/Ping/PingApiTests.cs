using System.Net;
using SecShare.Api.Dto.RequestResponse.Ping;
using SecShare.Tests.Integration.Api.Core;

namespace SecShare.Tests.Integration.Api.Ping;

public class PingApiTests : BaseTest
{
    private const string Url = "/api/ping";

    public PingApiTests(ApiCustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Ping_Returns_Ok_Status()
    {
        var response = await GetRequestAsAnonymousAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ping_Returns_Response_Body()
    {
        var response = await GetJsonAsAnonymousAsync<PingResponse>(Url);

        Assert.NotNull(response);
        Assert.Equal("ok", response.Status);
    }
}
