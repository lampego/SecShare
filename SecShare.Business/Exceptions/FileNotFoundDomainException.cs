using System.Net;

namespace SecShare.Business.Exceptions;

public class FileNotFoundDomainException : ApiException
{
    public FileNotFoundDomainException(string message = "Decrypted data is unavailable")
        : base(
            message,
            HttpStatusCode.NotFound
        )
    {
    }
}

