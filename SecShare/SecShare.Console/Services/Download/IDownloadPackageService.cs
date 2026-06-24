namespace SecShare.Console.Services.Download;

public interface IDownloadPackageService
{
    byte[] Decrypt(byte[] encryptedPayload, string encryptionKey);
}
