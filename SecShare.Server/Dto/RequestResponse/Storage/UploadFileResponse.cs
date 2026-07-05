using Api.Requests.Abstractions;

namespace SecShare.Server.Dto.RequestResponse.Storage;

public class UploadFileResponse : IResponse
{
    public string Token { get; init; } = string.Empty;
}
