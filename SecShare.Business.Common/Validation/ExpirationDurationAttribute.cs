using System.ComponentModel.DataAnnotations;
using SecShare.Business.Common.Dto.Storage;

namespace SecShare.Business.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ExpirationDurationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is string expires && UploadFileExpiration.TryParse(expires, out _))
        {
            return ValidationResult.Success;
        }

        var memberNames = validationContext.MemberName == null
            ? null
            : new[] { validationContext.MemberName };
        return new ValidationResult(
            ErrorMessage ?? UploadFileExpiration.ValidationErrorMessage,
            memberNames
        );
    }
}
