using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Http;

namespace SecShare.Api.Controllers.Storage.Actions;

public class UploadFileRequest : IRequest<UploadFileResponse>
{
    public IFormFile? File { get; set; }
    public string? Metadata { get; set; }
    public int? DeleteDelayInSeconds { get; set; }
}
