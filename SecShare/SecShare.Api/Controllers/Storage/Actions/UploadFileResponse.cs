using Api.Requests.Abstractions;

namespace SecShare.Api.Controllers.Storage.Actions;

public class UploadFileResponse : IResponse
{
    public string Token { get; init; } = string.Empty;
}
