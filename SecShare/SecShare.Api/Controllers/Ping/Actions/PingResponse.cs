using Api.Requests.Abstractions;

namespace SecShare.Api.Controllers.Ping.Actions;

public class PingResponse : IResponse
{
    public string Status { get; init; } = "ok";
}
