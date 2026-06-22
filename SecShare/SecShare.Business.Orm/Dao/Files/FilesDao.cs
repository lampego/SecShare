using Autofac;
using SecShare.Business.Orm.Dao.Common;
using SecShare.Business.Orm.Entities;

namespace SecShare.Business.Orm.Dao.Files;

public class FilesDao : BaseDao, IFilesDao
{
    public FilesDao(ILifetimeScope scope) : base(scope)
    {
    }

    public Task<FileEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Session.GetAsync<FileEntity?>(id, cancellationToken);
    }

    public async Task<FileEntity> CreateAsync(FileEntity file, CancellationToken cancellationToken = default)
    {
        await Session.SaveAsync(file, cancellationToken);
        return file;
    }
}
