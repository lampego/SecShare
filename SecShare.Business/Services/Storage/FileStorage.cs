using System.Net;
using Microsoft.AspNetCore.StaticFiles;
using SecShare.Business.Exceptions;
using SecShare.Business.Orm.Dao.Files;
using SecShare.Business.Orm.Entities;
using SecShare.Business.Services.Storage.Client;

namespace SecShare.Business.Services.Storage;

public class FileStorage : IFileStorage
{
    public const int MaxFileSize = 1024 * 1024 * 250;

    private readonly IFilesDao _filesDao;
    private readonly IFileStorageGarageClient _storageClient;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public FileStorage(IFilesDao filesDao, IFileStorageGarageClient storageClient)
    {
        _filesDao = filesDao;
        _storageClient = storageClient;
    }

    public async Task<FileEntity> PutFileAsync(
        byte[] fileData,
        string fileName,
        string? encryptionAlgorithm = null,
        string? encryptionKeyId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (fileData.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"File can not be larger than {MaxFileSize / 1024 / 1024}Mb");
        }

        var storagePath = BuildStoragePath(fileName);
        await using var fileStream = new MemoryStream(fileData);
        await _storageClient.Upload(storagePath, fileStream, cancellationToken);

        var file = new FileEntity
        {
            StoragePath = storagePath,
            Extension = Path.GetExtension(fileName).TrimStart('.'),
            MimeType = GetMimeType(fileName),
            OriginalFileName = fileName,
            Size = fileData.LongLength,
            EncryptionAlgorithm = encryptionAlgorithm,
            EncryptionKeyId = encryptionKeyId,
            CreatedAt = DateTime.UtcNow
        };

        var createdFile = await _filesDao.CreateAsync(file, cancellationToken);

        return createdFile;
    }

    public async Task<(FileEntity File, Stream FileStream)> GetFileStreamAsync(
        Guid fileId,
        CancellationToken cancellationToken = default
    )
    {
        var file = await _filesDao.GetAsync(fileId, cancellationToken)
            ?? throw new FileNotFoundDomainException("Decrypted data is unavailable");

        if (file.IsDeleted)
        {
            throw new FileDeletedDomainException("Decrypted data is unavailable");
        }

        return (file, await _storageClient.GetAsStream(file.StoragePath, cancellationToken));
    }

    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _filesDao.GetAsync(fileId, cancellationToken)
            ?? throw new InvalidOperationException($"File was not found: {fileId}");

        if (file.IsDeleted)
        {
            return;
        }

        await _storageClient.Delete(file.StoragePath, cancellationToken);
        file.DeletedAt = DateTime.UtcNow;
    }

    private static string BuildStoragePath(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return Path.Combine("files", DateTime.UtcNow.ToString("yyyyMMdd"), $"{Guid.CreateVersion7()}{extension}")
            .Replace("\\", "/");
    }

    private string GetMimeType(string fileName)
    {
        return _contentTypeProvider.TryGetContentType(fileName, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }
}
