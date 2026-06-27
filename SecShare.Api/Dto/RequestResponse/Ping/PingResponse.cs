using Api.Requests.Abstractions;

namespace SecShare.Api.Dto.RequestResponse.Ping;

public class PingResponse : IResponse
{
    public string Status { get; init; } = "ok";
}
