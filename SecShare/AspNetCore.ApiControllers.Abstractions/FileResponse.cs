using System.IO;
using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.ApiControllers.Abstractions;

public class FileResponse: FileStreamResult, IResponse
{
    public FileResponse(Stream fileStream, string contentType) : base(fileStream, contentType)
    {
    }

    public FileResponse(Stream fileStream, MediaTypeHeaderValue contentType) : base(fileStream, contentType)
    {
    }
}
