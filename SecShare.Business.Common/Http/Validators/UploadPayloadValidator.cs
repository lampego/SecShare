using SecShare.Business.Common.Formatting;

namespace SecShare.Business.Common.Http.Validators;

public static class UploadPayloadValidator
{
    public static void Validate(byte[] encryptedPayload)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);

        Validate(encryptedPayload.LongLength);
    }

    public static void Validate(long encryptedPayloadSizeBytes)
    {
        if (encryptedPayloadSizeBytes > TransferLimits.MaxUploadFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Encrypted payload size must not exceed {ByteSizeFormatter.Format(TransferLimits.MaxUploadFileSizeBytes)}."
            );
        }
    }
}