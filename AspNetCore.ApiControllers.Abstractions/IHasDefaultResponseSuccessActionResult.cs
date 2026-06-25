using System;
using Api.Requests.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.ApiControllers.Abstractions
{
    public interface IHasDefaultResponseSuccessActionResult
    {
        Func<TResponse, IActionResult> ResponseSuccess<TResponse>()
            where TResponse : IResponse;
    }
}