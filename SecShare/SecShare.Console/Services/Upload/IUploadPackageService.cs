using SecShare.Console.Services.Archive;

namespace SecShare.Console.Services.Upload;

public interface IUploadPackageService
{
    UploadPackage EncryptArchive(ZipArchiveBuildResult archive);
}
