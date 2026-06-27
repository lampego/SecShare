using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Http;
using SecShare.Api.Mvc.Attribute.Validation;

namespace SecShare.Api.Dto.RequestResponse.Storage;

public class UploadFileRequest : IRequest<UploadFileResponse>
{
    /// <summary>
    /// Required multipart file content. The uploaded file must contain at least one byte.
    /// </summary>
    [RequiredNonEmpty(ErrorMessage = "File is required and must contain data.")]
    public IFormFile? File { get; set; }

    /// <summary>
    /// Optional metadata encoded as a JSON string. When supplied, it must be valid JSON.
    /// </summary>
    [IsJson(ErrorMessage = "Metadata must be a valid JSON string when provided.")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Optional lifetime in seconds before the uploaded file is deleted. When supplied, it must be greater than zero.
    /// </summary>
    [IsPositive(ErrorMessage = "DeleteDelayInSeconds must be greater than zero when provided.")]
    public int? DeleteDelayInSeconds { get; set; }
}
