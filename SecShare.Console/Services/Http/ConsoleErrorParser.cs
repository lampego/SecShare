using SecShare.Business.Exceptions;

namespace SecShare.Console.Services.Http;

public static class ConsoleErrorParser
{
    public static string ResolveFriendlyDownloadErrorMessage(Exception exception)
    {
        return exception switch
        {
            FileNotFoundDomainException => "The file was not found on the server.",
            FileDeletedDomainException => "This file has been deleted and is no longer available.",
            DownloadLimitExhaustedDomainException => "The download limit for this file has been exhausted.",
            UploadOptionsValidationDomainException => $"Invalid upload options: {exception.Message}",
            InvalidDataException => "The downloaded archive is corrupted or uses an unsupported format.",
            IOException ex when ex.Message.Contains("already contains", StringComparison.OrdinalIgnoreCase) =>
                "The destination already contains files or directories from this archive.",
            IOException ex when ex.Message.Contains("existing file", StringComparison.OrdinalIgnoreCase) =>
                "The destination path points to an existing file. Choose a directory path instead.",
            _ when exception.Message == "Server Exception" => "Decrypted data is unavailable.",
            _ => exception.Message
        };
    }

    public static string ResolveFriendlyUploadErrorMessage(Exception exception)
    {
        return exception switch
        {
            UploadOptionsValidationDomainException => $"Invalid upload options: {exception.Message}",
            DataValidationException => exception.Message,
            _ when exception.Message == "Server Exception" => "Failed to upload file due to a server error.",
            _ => exception.Message
        };
    }
}
