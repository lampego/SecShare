using System.Net;

namespace SecShare.Business.Exceptions;

public class FileDeletedDomainException : ApiException
{
    public FileDeletedDomainException(string message = "Decrypted data is unavailable")
        : base(
            message,
            HttpStatusCode.NotFound
        )
    {
    }
}

