using SecShare.Console.Models.Archive;
using SecShare.Console.Models.Upload;

namespace SecShare.Console.Services.Upload;

public interface IUploadPackageService
{
    UploadPackage EncryptArchive(ZipArchiveBuildResult archive);
}
