using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SecShare.Api.Common.Dto.Storage;
using SecShare.Api.Dto.RequestResponse.Storage;

namespace SecShare.Tests.Unit.Api.Storage;

public sealed class UploadFileRequestValidationTests
{
    [Fact]
    public void Validate_WithMissingFile_ReturnsFileRequiredError()
    {
        var request = new UploadFileRequest
        {
            Options = CreateOptions()
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.File)));
    }

    [Fact]
    public void Validate_WithEmptyFile_ReturnsFileRequiredError()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([]),
            Options = CreateOptions()
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.File)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_WithInvalidDownloads_ReturnsDownloadsError(int downloads)
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = CreateOptions(downloads: downloads)
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileOptions.Downloads)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0h")]
    [InlineData("24")]
    [InlineData("1w")]
    [InlineData("999999d")]
    [InlineData("999999999999999999999999d")]
    public void Validate_WithInvalidExpires_ReturnsExpiresError(string expires)
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = CreateOptions(expires: expires)
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileOptions.Expires)));
    }

    [Theory]
    [InlineData("365d")]
    [InlineData("8760h")]
    [InlineData("525600m")]
    [InlineData("31536000s")]
    public void Validate_WithBoundaryValidExpires_ReturnsNoExpiresError(string expires)
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = CreateOptions(expires: expires)
        };

        var results = Validate(request);

        Assert.DoesNotContain(results, result => result.MemberNames.Contains(nameof(UploadFileOptions.Expires)));
    }

    [Fact]
    public void UploadFileExpiration_TryParse_WithInvalidValues_ReturnsFalse()
    {
        Assert.False(UploadFileExpiration.TryParse("", out _));
        Assert.False(UploadFileExpiration.TryParse("1w", out _));
        Assert.False(UploadFileExpiration.TryParse("999999d", out _));
        Assert.False(UploadFileExpiration.TryParse("999999999999999999999999d", out _));
    }

    [Fact]
    public void Validate_WithMissingOptions_ReturnsOptionsError()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1])
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UploadFileRequest.Options)));
    }

    [Fact]
    public void Validate_WithRequiredFileAndOptions_ReturnsNoErrors()
    {
        var request = new UploadFileRequest
        {
            File = CreateFormFile([1]),
            Options = CreateOptions()
        };

        var results = Validate(request);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(UploadFileRequest request)
    {
        var results = new List<ValidationResult>();

        TryValidateObject(
            request,
            new ValidationContext(request),
            results
        );
        if (request.Options != null)
        {
            TryValidateObject(
                request.Options,
                new ValidationContext(request.Options),
                results
            );
        }

        return results;
    }

    private static void TryValidateObject(
        object instance,
        ValidationContext validationContext,
        ICollection<ValidationResult> results
    )
    {
        Validator.TryValidateObject(
            instance,
            validationContext,
            results,
            validateAllProperties: true
        );
    }

    private static UploadFileOptions CreateOptions(
        string expires = "24h",
        int downloads = 1,
        bool hasPassword = false
    )
    {
        return new UploadFileOptions
        {
            Expires = expires,
            Downloads = downloads,
            HasPassword = hasPassword
        };
    }

    private static IFormFile CreateFormFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, nameof(UploadFileRequest.File), "test.txt");
    }
}
