using System.Net;

namespace SecShare.Business.Exceptions;

public class DataValidationException : ApiException
{
    public DataValidationException(string message)
        : base(
            message,
            HttpStatusCode.BadRequest
        )
    {
    }
}
