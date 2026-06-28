using System.Net;

namespace SecShare.Business.Exceptions;

public class DownloadLimitExhaustedDomainException : ApiException
{
    public DownloadLimitExhaustedDomainException(string message = "Decrypted data is unavailable")
        : base(
            message,
            HttpStatusCode.NotFound
        )
    {
    }
}

