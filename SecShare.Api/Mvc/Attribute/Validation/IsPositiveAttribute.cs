using System.ComponentModel.DataAnnotations;

namespace SecShare.Api.Mvc.Attribute.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class IsPositiveAttribute : ValidationAttribute
{
    public bool AllowZero { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (IsPositive(value))
        {
            return ValidationResult.Success;
        }

        var memberNames = validationContext.MemberName == null
            ? null
            : new[] { validationContext.MemberName };
        var errorMessage = ErrorMessage
            ?? $"The {validationContext.DisplayName} field must be {(AllowZero ? "zero or a positive number" : "a positive number")}.";

        return new ValidationResult(errorMessage, memberNames);
    }

    private bool IsPositive(object value)
    {
        return value switch
        {
            byte number => IsAllowed((decimal)number),
            short number => IsAllowed((decimal)number),
            int number => IsAllowed((decimal)number),
            long number => IsAllowed((decimal)number),
            sbyte number => IsAllowed((decimal)number),
            ushort number => IsAllowed((decimal)number),
            uint number => IsAllowed((decimal)number),
            ulong number => IsAllowed((decimal)number),
            float number => !float.IsNaN(number) && IsAllowed(number),
            double number => !double.IsNaN(number) && IsAllowed(number),
            decimal number => IsAllowed(number),
            _ => false
        };
    }

    private bool IsAllowed(decimal value)
    {
        return AllowZero
            ? value >= 0
            : value > 0;
    }

    private bool IsAllowed(double value)
    {
        return AllowZero
            ? value >= 0
            : value > 0;
    }
}
