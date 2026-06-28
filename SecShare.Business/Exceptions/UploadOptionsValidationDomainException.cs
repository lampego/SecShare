using System.Net;

namespace SecShare.Business.Exceptions;

public class UploadOptionsValidationDomainException : ApiException
{
    public UploadOptionsValidationDomainException(string message)
        : base(
            message,
            HttpStatusCode.BadRequest
        )
    {
    }
}

