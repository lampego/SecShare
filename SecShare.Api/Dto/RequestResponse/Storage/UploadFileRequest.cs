using System.ComponentModel.DataAnnotations;
using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Http;
using SecShare.Api.Common.Dto.Storage;
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
    /// Required upload options supplied as multipart form fields under the "Options" prefix.
    /// </summary>
    [Required(ErrorMessage = "Options are required.")]
    public UploadFileOptions? Options { get; set; }
}
