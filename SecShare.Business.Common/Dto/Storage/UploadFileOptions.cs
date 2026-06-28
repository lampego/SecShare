using System.ComponentModel.DataAnnotations;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Validation;

namespace SecShare.Business.Common.Dto.Storage;

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

    [EnumDataType(typeof(StorageContentType))]
    public StorageContentType ContentType { get; set; } = StorageContentType.File;
}
