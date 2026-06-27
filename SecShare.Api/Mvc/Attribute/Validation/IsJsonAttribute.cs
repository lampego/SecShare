using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SecShare.Api.Mvc.Attribute.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class IsJsonAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return ValidationResult.Success;
            }

            try
            {
                using var _ = JsonDocument.Parse(json);
                return ValidationResult.Success;
            }
            catch (JsonException)
            {
                return GetError(validationContext);
            }
        }

        return GetError(validationContext);
    }

    private ValidationResult GetError(ValidationContext validationContext)
    {
        var memberNames = validationContext.MemberName == null
            ? null
            : new[] { validationContext.MemberName };
        var errorMessage = ErrorMessage
            ?? $"The {validationContext.DisplayName} field must contain valid JSON.";

        return new ValidationResult(errorMessage, memberNames);
    }
}
