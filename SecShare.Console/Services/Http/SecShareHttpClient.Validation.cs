using System.ComponentModel.DataAnnotations;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
        => await SecShareHttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

    private static void ValidateUploadOptions(UploadFileOptions options)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(
                options,
                new ValidationContext(options),
                results,
                validateAllProperties: true
            )
        )
        {
            return;
        }

        throw new ArgumentException(SecShareHttpErrorParser.FormatValidationErrors(results));
    }
}
