using Api.Requests.Abstractions;
using SecShare.Api.Dto.RequestResponse.Ping;

namespace SecShare.Api.Controllers.Ping.Actions;

public class PingRequestHandler : IAsyncRequestHandler<PingRequest, PingResponse>
{
    public Task<PingResponse> ExecuteAsync(PingRequest request)
    {
        return Task.FromResult(new PingResponse());
    }
}
