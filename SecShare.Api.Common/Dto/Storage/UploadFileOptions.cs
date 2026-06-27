using System.ComponentModel.DataAnnotations;
using SecShare.Api.Common.Validation;

namespace SecShare.Api.Common.Dto.Storage;

public class UploadFileOptions
{
    /// <summary>
    /// Positive expiration duration. Supported suffixes are "s", "m", "h", and "d".
    /// </summary>
    [Required]
    [ExpirationDuration]
    public string Expires { get; set; } = "24h";

    /// <summary>
    /// Maximum number of successful downloads allowed for this upload.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Downloads must be greater than zero.")]
    public int Downloads { get; set; } = 1;

    /// <summary>
    /// Indicates whether the client requires an additional password before decryption.
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// Original source name shown to users and used to name the encrypted upload payload.
    /// </summary>
    [Required]
    [StringLength(128, ErrorMessage = "SourceName must be 128 characters or fewer.")]
    [RegularExpression(@"^[^<>:""/\\|?*\x00-\x1F]+$", ErrorMessage = "SourceName must be a safe file name without path separators or control characters.")]
    public string SourceName { get; set; } = string.Empty;
}
