using Api.Requests.Abstractions;

namespace SecShare.Api.Controllers.Ping.Actions;

public class PingRequestHandler : IAsyncRequestHandler<PingRequest, PingResponse>
{
    public Task<PingResponse> ExecuteAsync(PingRequest request)
    {
        return Task.FromResult(new PingResponse());
    }
}
