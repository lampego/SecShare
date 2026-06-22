using SecShare.Business.Orm.Entities;
using SecShare.Business.Orm.Mapping.Common;

namespace SecShare.Business.Orm.Mapping.Entities;

public class FileMapping : BaseGuidMappings<FileEntity>
{
    public FileMapping()
    {
        Table("files");

        Map(x => x.StoragePath).Not.Nullable();
        Map(x => x.Extension).Nullable();
        Map(x => x.MimeType).Not.Nullable();
        Map(x => x.OriginalFileName).Not.Nullable();
        Map(x => x.Size).Not.Nullable();
        Map(x => x.EncryptionAlgorithm).Nullable();
        Map(x => x.EncryptionKeyId).Nullable();
        Map(x => x.CreatedAt).Not.Nullable();
        Map(x => x.UpdatedAt).Nullable();
        Map(x => x.DeletedAt).Nullable();
    }
}
