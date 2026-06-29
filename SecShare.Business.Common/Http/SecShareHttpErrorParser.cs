using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using SecShare.Business.Dto;
using SecShare.Business.Exceptions;

namespace SecShare.Business.Common.Http;

public static class SecShareHttpErrorParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (TryParseCommonError(errorContent, response.StatusCode, out var commonError))
        {
            throw commonError;
        }
        if (TryParseValidationError(errorContent, response.StatusCode, out var validationError))
        {
            throw validationError;
        }

        throw new HttpRequestException(
            $"Request failed with status {(int)response.StatusCode}.",
            inner: null,
            statusCode: response.StatusCode);
    }

    public static bool TryParseCommonError(
        string errorContent,
        HttpStatusCode statusCode,
        out ApiException exception
    )
    {
        exception = null!;
        try
        {
            var errorResult = JsonSerializer.Deserialize<JsonCommonResponse>(
                errorContent,
                JsonOptions
            );
            if (errorResult is not { Status: "fail" })
            {
                return false;
            }

            exception = ParseExceptionFromResponse(
                errorResult,
                statusCode
            );
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static ApiException ParseExceptionFromResponse(
        JsonCommonResponse errorResult,
        HttpStatusCode statusCode
    )
    {
        return errorResult.ErrorCode switch
        {
            "FileNotFoundDomainException" => new FileNotFoundDomainException(errorResult.Message),
            "FileDeletedDomainException" => new FileDeletedDomainException(errorResult.Message),
            "DownloadLimitExhaustedDomainException" => new DownloadLimitExhaustedDomainException(errorResult.Message),
            "UploadOptionsValidationDomainException" => new UploadOptionsValidationDomainException(errorResult.Message),
            _ => new ApiException(
                errorResult.Message,
                statusCode
            )
        };
    }

    public static bool TryParseValidationError(
        string errorContent,
        HttpStatusCode statusCode,
        out ApiException exception
    )
    {
        exception = null!;
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
            exception = new ApiException(
                FormatValidationErrors(results),
                statusCode
            );
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string FormatValidationErrors(IEnumerable<ValidationResult> results)
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
