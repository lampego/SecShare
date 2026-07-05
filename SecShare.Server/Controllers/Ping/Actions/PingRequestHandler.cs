using Api.Requests.Abstractions;
using SecShare.Server.Dto.RequestResponse.Ping;

namespace SecShare.Server.Controllers.Ping.Actions;

public class PingRequestHandler : IAsyncRequestHandler<PingRequest, PingResponse>
{
    public Task<PingResponse> ExecuteAsync(PingRequest request)
    {
        return Task.FromResult(new PingResponse());
    }
}
