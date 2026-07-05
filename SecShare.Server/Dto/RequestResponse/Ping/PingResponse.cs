using Api.Requests.Abstractions;

namespace SecShare.Server.Dto.RequestResponse.Ping;

public class PingResponse : IResponse
{
    public string Status { get; init; } = "ok";
}
