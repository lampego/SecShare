using AspNetCore.ApiControllers.Abstractions;
using AspNetCore.ApiControllers.Extensions;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Business.Mvc.Controllers;

namespace SecShare.Api.Controllers.Storage;

[ApiController]
public class StorageController(ILifetimeScope scope) : MainApiControllerBase(scope)
{
    [HttpPost("api/file/upload")]
    [HttpPost("api/files")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Upload([FromForm] UploadFileRequest request)
    {
        return this.RequestAsync()
            .For<UploadFileResponse>()
            .With(request);
    }

    [HttpGet("api/file/get/{id}")]
    [HttpGet("api/files/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(string id)
    {
        return this.RequestAsync()
            .For<FileResponse>()
            .With(new GetFileRequest { Id = id });
    }
}
