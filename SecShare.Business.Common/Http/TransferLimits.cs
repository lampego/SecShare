namespace SecShare.Business.Common.Http;

public static class TransferLimits
{
    public const long MaxMultipartRequestBodySizeBytes = 100L * 1024 * 1024;
    public const long MultipartFormDataOverheadBytes = 1L * 1024 * 1024;
    public const long MaxUploadFileSizeBytes = MaxMultipartRequestBodySizeBytes - MultipartFormDataOverheadBytes;
}