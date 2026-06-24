using System.Net;
using Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecShare.Business.Dto;
using SecShare.Business.Exceptions;

namespace SecShare.Business.Mvc.Middleware;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var isDomain = ex is IDomainException;
            var statusCode = isDomain
                ? (ex is ApiException apiEx ? apiEx.StatusCode : HttpStatusCode.BadRequest)
                : HttpStatusCode.InternalServerError;

            if (!isDomain)
            {
                logger.LogError(ex, ex.Message);
            }

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)statusCode;

                var responseObj = new JsonCommonResponse
                {
                    Status = "fail",
                    Message = isDomain ? ex.Message : "Server Exception",
                    ErrorCode = isDomain ? ex.GetType().Name : "ServerException"
                };

                await context.Response.WriteAsJsonAsync(responseObj);
            }
            else
            {
                logger.LogWarning(ex, "Response has already started, cannot write exception to response.");
            }
        }
    }
}
