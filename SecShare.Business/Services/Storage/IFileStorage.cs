using Domain.Abstractions;
using SecShare.Business.Orm.Entities;

namespace SecShare.Business.Services.Storage;

public interface IFileStorage : IDomainService
{
    Task<FileEntity> PutFileAsync(
        byte[] fileData,
        string fileName,
        string? encryptionAlgorithm = null,
        string? encryptionKeyId = null,
        CancellationToken cancellationToken = default
    );

    Task<(FileEntity File, Stream FileStream)> GetFileStreamAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
}
