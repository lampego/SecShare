using Domain.Abstractions;
using SecShare.Business.Orm.Entities;

namespace SecShare.Business.Orm.Dao.Files;

public interface IFilesDao : IDomainService
{
    Task<FileEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FileEntity> CreateAsync(FileEntity file, CancellationToken cancellationToken = default);
    Task<int?> ConsumeDownloadAsync(Guid id, CancellationToken cancellationToken = default);
}
