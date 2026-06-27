using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SecShare.Api.Dto.RequestResponse.Storage;

namespace SecShare.Tests.Unit.Api.Storage;

public sealed class UploadFileRequestValidationTests
{
    [Fact]
    public void Validate_WithMissingFile_ReturnsFileRequiredError()
    {
        var request = new UploadFileRequest();

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.File)));
    }

    [Fact]
    public void Validate_WithEmptyFile_ReturnsFileRequiredError()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([])
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.File)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_WithNonPositiveDeleteDelay_ReturnsDeleteDelayError(int deleteDelayInSeconds)
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            DeleteDelayInSeconds = deleteDelayInSeconds
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.DeleteDelayInSeconds)));
    }

    [Fact]
    public void Validate_WithInvalidMetadata_ReturnsMetadataError()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Metadata = "{invalid"
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.Metadata)));
    }

    [Fact]
    public void Validate_WithRequiredValuesAndPositiveDeleteDelay_ReturnsNoErrors()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Metadata = "{\"source\":\"console\"}",
            DeleteDelayInSeconds = 1
        };

        var results = Validate(request);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(UploadFileRequest request)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true
        );

        return results;
    }

    private static IFormFile CreateFormFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, nameof(UploadFileRequest.File), "test.txt");
    }
}
