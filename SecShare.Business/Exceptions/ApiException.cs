using System.Net;
using Domain.Abstractions;

namespace SecShare.Business.Exceptions;

public class ApiException : Exception, IDomainException
{
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.BadRequest;

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiException(string message, HttpStatusCode statusCode, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
