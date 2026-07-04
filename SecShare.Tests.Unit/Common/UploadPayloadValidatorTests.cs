using SecShare.Business.Common.Http;
using SecShare.Business.Common.Http.Validators;

namespace SecShare.Tests.Unit.Common;

public sealed class UploadPayloadValidatorTests
{
    [Fact]
    public void Validate_WhenPayloadFitsLimit_DoesNotThrow()
    {
        var exception = Record.Exception(
            () => UploadPayloadValidator.Validate(TransferLimits.MaxUploadFileSizeBytes)
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenPayloadExceedsLimit_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => UploadPayloadValidator.Validate(TransferLimits.MaxUploadFileSizeBytes + 1)
        );

        Assert.Equal("Encrypted payload size must not exceed 99 MB.", exception.Message);
    }
}