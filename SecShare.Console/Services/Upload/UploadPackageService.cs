using SecShare.Business.Services.Crypto;
using SecShare.Business.Common.Models.Archive;
using SecShare.Console.Models.Upload;

namespace SecShare.Console.Services.Upload;

public sealed class UploadPackageService(CryptoService cryptoService) : IUploadPackageService
{
    public UploadPackage EncryptArchive(ZipArchiveBuildResult archive)
    {
        var (encryptedPayload, encryptionKey) = cryptoService.Encrypt(archive.ArchiveBytes);

        return new UploadPackage(
            encryptedPayload,
            encryptionKey,
            archive.SourceSizeBytes,
            archive.ArchiveBytes.LongLength,
            archive.FileCount,
            archive.SourceName
        );
    }
}
