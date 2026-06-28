using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Dto;
using SecShare.Business.Exceptions;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (TryReadCommonError(errorContent, response.StatusCode, out var commonError))
        {
            throw commonError;
        }
        if (TryReadValidationError(errorContent, response.StatusCode, out var validationError))
        {
            throw validationError;
        }

        response.EnsureSuccessStatusCode();
    }

    private static void ValidateUploadOptions(UploadFileOptions options)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(
                options,
                new ValidationContext(options),
                results,
                validateAllProperties: true
            ))
        {
            return;
        }

        throw new ArgumentException(FormatValidationErrors(results));
    }

    private static bool TryReadCommonError(
        string errorContent,
        HttpStatusCode statusCode,
        out ApiException error
    )
    {
        var isParsed = ConsoleErrorParser.TryParseCommonError(
            errorContent,
            statusCode,
            out var parsedException
        );
        error = parsedException!;
        return isParsed;
    }

    private static bool TryReadValidationError(
        string errorContent,
        HttpStatusCode statusCode,
        out ApiException error
    )
    {
        error = null!;
        try
        {
            var validationErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
                errorContent,
                JsonOptions
            );
            if (validationErrors == null || validationErrors.Count == 0)
            {
                return false;
            }

            var results = validationErrors
                .SelectMany(item => item.Value.Select(message => new ValidationResult(message, [item.Key])))
                .ToArray();
            error = new ApiException(FormatValidationErrors(results), statusCode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string FormatValidationErrors(IEnumerable<ValidationResult> results)
    {
        return string.Join(
            Environment.NewLine,
            results
                .Select(FormatValidationError)
                .Where(message => !string.IsNullOrWhiteSpace(message))
        );
    }

    private static string FormatValidationError(ValidationResult result)
    {
        var message = result.ErrorMessage ?? string.Empty;
        var memberName = result.MemberNames.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return message;
        }

        return $"{memberName}: {message}";
    }
}
