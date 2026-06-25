using SecShare.Business.Orm.Core;

namespace SecShare.Business.Orm.Entities;

public class FileEntity : AEntity
{
    public virtual required string StoragePath { get; set; }
    public virtual string? Extension { get; set; }
    public virtual required string MimeType { get; set; }
    public virtual required string OriginalFileName { get; set; }
    public virtual long Size { get; set; }
    public virtual string? EncryptionAlgorithm { get; set; }
    public virtual string? EncryptionKeyId { get; set; }
    public virtual DateTime? DeleteAt { get; set; }
}
