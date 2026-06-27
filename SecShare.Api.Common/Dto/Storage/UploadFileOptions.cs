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

}
