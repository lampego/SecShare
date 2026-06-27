using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;

namespace SecShare.Api.Dto.RequestResponse.Storage;

public class GetFileRequest : IRequest<FileResponse>
{
    public string Id { get; set; } = string.Empty;
}
