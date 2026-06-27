using SecShare.Console.Models.Download;

namespace SecShare.Console.Services.Download;

public sealed class DecryptionKeyResolver(IDecryptionKeyReader reader)
{
    public string Resolve(SecShareDownloadLink link)
    {
        ArgumentNullException.ThrowIfNull(link);

        if (!string.IsNullOrWhiteSpace(link.EncryptionKey))
        {
            return link.EncryptionKey;
        }

        var decryptionKey = reader.ReadDecryptionKey();
        if (string.IsNullOrWhiteSpace(decryptionKey))
        {
            throw new ArgumentException("Decryption key is required.");
        }

        return decryptionKey;
    }
}
