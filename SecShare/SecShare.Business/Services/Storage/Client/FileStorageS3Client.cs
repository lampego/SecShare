using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecShare.Business.Services.Storage.Client;

public class FileStorageS3Client : IFileStorageS3Client
{
    private readonly ILogger<FileStorageS3Client> _logger;
    private readonly string _bucketName;
    private readonly AmazonS3Client _s3Client;

    public FileStorageS3Client(
        IConfiguration configuration,
        ILogger<FileStorageS3Client> logger
    )
    {
        _logger = logger;

        var accessKey = configuration.GetValue<string>("AWS:S3:AccessKey")
            ?? throw new ArgumentNullException("AWS:S3:AccessKey");
        var secretKey = configuration.GetValue<string>("AWS:S3:SecretKey")
            ?? throw new ArgumentNullException("AWS:S3:SecretKey");
        _bucketName = configuration.GetValue<string>("AWS:S3:BucketName")
            ?? throw new ArgumentNullException("AWS:S3:BucketName");

        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.EUCentral1,
            DisableLogging = true,
            BufferSize = 65536,
            DefaultConfigurationMode = DefaultConfigurationMode.InRegion,
            UseFIPSEndpoint = false,
            ProgressUpdateInterval = 1024 * 1024
        };

        _s3Client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
    }

    public async Task<UploadedFileDto?> Upload(
        string filePath,
        Stream fileStream,
        CancellationToken cancellationToken = default
    )
    {
        var s3Request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = filePath,
            InputStream = fileStream,
            AutoCloseStream = false,
            StreamTransferProgress = (_, args) =>
            {
                _logger.LogTrace("S3 file uploading progress: {PercentDone}%", args.PercentDone);
            }
        };

        _logger.LogDebug("S3 file uploading started: {FilePath}", filePath);
        var response = await _s3Client.PutObjectAsync(s3Request, cancellationToken);
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            _logger.LogDebug("S3 file uploading finished: {FilePath}", filePath);
            return new UploadedFileDto();
        }

        throw new InvalidOperationException($"File uploading error via S3 client: {response.HttpStatusCode}");
    }

    public async Task<Stream> GetAsStream(string filePath, CancellationToken cancellationToken = default)
    {
        using var response = await _s3Client.GetObjectAsync(_bucketName, filePath, cancellationToken);
        var fileStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(fileStream, cancellationToken);
        fileStream.Position = 0;
        return fileStream;
    }

    public Task Delete(string filePath, CancellationToken cancellationToken = default)
    {
        return _s3Client.DeleteObjectAsync(_bucketName, filePath, cancellationToken);
    }
}
