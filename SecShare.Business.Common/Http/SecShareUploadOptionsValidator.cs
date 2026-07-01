using System.ComponentModel.DataAnnotations;
using SecShare.Business.Common.Dto.Storage;

namespace SecShare.Business.Common.Http;

public static class SecShareUploadOptionsValidator
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

        throw new ArgumentException(SecShareHttpErrorParser.FormatValidationErrors(results));
    }
}
