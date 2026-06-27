using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SecShare.Api.Mvc.Attribute.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredNonEmptyAttribute : RequiredAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return false;
        }

        if (value is IFormFile formFile)
        {
            return formFile.Length > 0;
        }

        if (value is Guid guidValue)
        {
            return guidValue != Guid.Empty;
        }

        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }

        return base.IsValid(value);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (IsValid(value))
        {
            return ValidationResult.Success;
        }

        var memberNames = validationContext.MemberName == null
            ? null
            : new[] { validationContext.MemberName };
        var errorMessage = ErrorMessage
            ?? $"The {validationContext.DisplayName} field is required and cannot be empty.";

        return new ValidationResult(errorMessage, memberNames);
    }
}
