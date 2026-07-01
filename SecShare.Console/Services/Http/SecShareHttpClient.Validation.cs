using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Http;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
        => await SecShareHttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

    private static void ValidateUploadOptions(UploadFileOptions options)
        => SecShareUploadOptionsValidator.Validate(options);
}
