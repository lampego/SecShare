using System;
using System.Net;
using System.Text.Json;
using SecShare.Business.Dto;
using SecShare.Business.Exceptions;

namespace SecShare.Console.Services.Http;

public static class ConsoleErrorParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParseCommonError(
        string errorContent,
        HttpStatusCode statusCode,
        out ApiException? exception
    )
    {
        exception = null;
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

    public static string ResolveFriendlyDownloadErrorMessage(Exception exception)
    {
        return exception switch
        {
            FileNotFoundDomainException => "The file was not found on the server.",
            FileDeletedDomainException => "This file has been deleted and is no longer available.",
            DownloadLimitExhaustedDomainException => "The download limit for this file has been exhausted.",
            UploadOptionsValidationDomainException => $"Invalid upload options: {exception.Message}",
            _ when exception.Message == "Server Exception" => "Decrypted data is unavailable.",
            _ => exception.Message
        };
    }

    public static string ResolveFriendlyUploadErrorMessage(Exception exception)
    {
        return exception switch
        {
            UploadOptionsValidationDomainException => $"Invalid upload options: {exception.Message}",
            _ when exception.Message == "Server Exception" => "Failed to upload file due to a server error.",
            _ => exception.Message
        };
    }
}

