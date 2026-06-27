using Autofac;
using NHibernate;
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

    public Task<int?> ConsumeDownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Session.CreateSQLQuery(
                """
                update files
                set downloads_remaining = downloads_remaining - 1,
                    updated_at = :updatedAt
                where id = :id
                    and deleted_at is null
                    and downloads_remaining > 0
                returning downloads_remaining
                """
            )
            .AddScalar("downloads_remaining", NHibernateUtil.Int32)
            .SetParameter("id", id)
            .SetParameter("updatedAt", DateTime.UtcNow)
            .UniqueResultAsync<int?>(cancellationToken);
    }
}
