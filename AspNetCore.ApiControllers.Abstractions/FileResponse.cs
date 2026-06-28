using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.ApiControllers.Abstractions;

public class FileResponse : FileStreamResult, IResponse
{
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    public FileResponse(Stream fileStream, string contentType) : base(fileStream, contentType)
    {
    }

    public FileResponse(Stream fileStream, MediaTypeHeaderValue contentType) : base(fileStream, contentType)
    {
    }

    public override Task ExecuteResultAsync(ActionContext context)
    {
        foreach (var (name, value) in Headers)
        {
            context.HttpContext.Response.Headers[name] = value;
        }

        return base.ExecuteResultAsync(context);
    }
}
