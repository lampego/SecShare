using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Http;
using SecShare.Business.Common.Http.Parsers;
using SecShare.Business.Common.Http.Validators;

namespace SecShare.Console.Services.Http;

public sealed partial class SecShareHttpClient
{
    private async Task EnsureSuccessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
        => await HttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

    private static void ValidateUploadOptions(UploadFileOptions options)
        => UploadOptionsValidator.Validate(options);
}
