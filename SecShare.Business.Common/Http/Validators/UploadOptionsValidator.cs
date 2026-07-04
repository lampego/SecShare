using System.ComponentModel.DataAnnotations;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Http.Parsers;

namespace SecShare.Business.Common.Http.Validators;

public static class UploadOptionsValidator
{
    public static void Validate(UploadFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

        throw new ArgumentException(HttpErrorParser.FormatValidationErrors(results));
    }
}