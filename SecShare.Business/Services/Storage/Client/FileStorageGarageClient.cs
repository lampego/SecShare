using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecShare.Business.Services.Storage.Client;

public class FileStorageGarageClient : IFileStorageGarageClient
{
    private readonly ILogger<FileStorageGarageClient> _logger;
    private readonly string _bucketName;
    private readonly AmazonS3Client _s3Client;

    public FileStorageGarageClient(
        IConfiguration configuration,
        ILogger<FileStorageGarageClient> logger
    )
    {
        _logger = logger;

        var accessKey = configuration.GetValue<string>("Garage:AccessKey")
            ?? throw new ArgumentNullException("Garage:AccessKey");
        var secretKey = configuration.GetValue<string>("Garage:SecretKey")
            ?? throw new ArgumentNullException("Garage:SecretKey");
        _bucketName = configuration.GetValue<string>("Garage:BucketName")
            ?? throw new ArgumentNullException("Garage:BucketName");
        var url = configuration.GetValue<string>("Garage:Url")
            ?? throw new ArgumentNullException("Garage:Url");

        var config = new AmazonS3Config
        {
            ServiceURL = url,
            ForcePathStyle = true, // Important for Garage
            DisableLogging = true,
            BufferSize = 65536,
            ProgressUpdateInterval = 1024 * 1024,
            AuthenticationRegion = "garage",
            SignatureMethod = SigningAlgorithm.HmacSHA256
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
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
            DisableDefaultChecksumValidation = true,
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
