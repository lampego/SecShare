using System.Net;
using Api.Requests.Abstractions;
using AspNetCore.ApiControllers.Abstractions;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecShare.Business.Dto;
using SecShare.Business.Exceptions;

namespace SecShare.Business.Mvc.Controllers;

public class MainApiControllerBase : ApiControllerBase
{
    protected readonly ILogger<MainApiControllerBase> Logger;
    protected readonly IHttpContextAccessor HttpContextAccessor;

    public MainApiControllerBase(ILifetimeScope scope)
        : base(scope.Resolve<IAsyncRequestBuilder>())
    {
        Logger = scope.Resolve<ILogger<MainApiControllerBase>>();
        HttpContextAccessor = scope.Resolve<IHttpContextAccessor>();

        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            if (this.GetType().Name.Contains("StorageController", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidConsoleRequest(httpContext))
                {
                    throw new ApiException("Forbidden: Requests are only allowed from the SecShare Console application.", HttpStatusCode.Forbidden);
                }
            }
        }
    }

    public override Func<IActionResult> Success => () =>
    {
        var httpResponse = HttpContextAccessor.HttpContext?.Response;
        if (httpResponse != null)
        {
            // If Redirect was completed in action
            if (httpResponse.StatusCode is StatusCodes.Status302Found)
            {
                var redirectUrl = httpResponse.Headers.Location!.FirstOrDefault();
                if (string.IsNullOrEmpty(redirectUrl))
                    throw new Exception("Redirect URL was not configured but status code yes");
                return Redirect(redirectUrl);
            }
            if (httpResponse.StatusCode is StatusCodes.Status301MovedPermanently)
            {
                var redirectUrl = httpResponse.Headers.Location!.FirstOrDefault();
                if (string.IsNullOrEmpty(redirectUrl))
                    throw new Exception("Redirect URL was not configured but status code yes");
                return RedirectPermanent(redirectUrl);
            }
        }
        return new OkObjectResult(new JsonCommonResponse { Status = "ok" });
    };

    protected JsonResult JsonSuccess(object? data = null, HttpStatusCode code = HttpStatusCode.OK, string? message = null)
    {
        var response = new JsonResult(
            new JsonCommonResponse()
            {
                Status = "ok",
                Message = message ?? string.Empty,
                Data = data ?? new { }
            }
        );
        response.StatusCode = (int)code;
        return response;
    }

    protected bool IsValidConsoleRequest(HttpContext? httpContext = null)
    {
        var context = httpContext ?? HttpContextAccessor.HttpContext;
        if (context == null) return false;

        var request = context.Request;

        // Check custom header
        if (request.Headers.TryGetValue("X-Client-Type", out var clientType) &&
            string.Equals(clientType, "Console", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check user-agent
        if (request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            var uaString = userAgent.ToString();
            if (uaString.StartsWith("SecShareConsole", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
