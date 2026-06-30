using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Common.Services.Archive;
using SecShare.Web.Services.Crypto;

namespace SecShare.Web.Pages.Upload;

public partial class Upload : IAsyncDisposable
{
    private const int MaxSelectedFiles = 100;
    private const long MaxSourceSizeBytes = ZipArchiveService.MaxSourceSizeBytes;
    private const string ApiFilesPath = "/api/files";
    private const string EncryptedUploadFileName = "secret_file";

    private sealed record SelectedUploadFile(Guid Id, IBrowserFile File);
    private sealed record UploadResultViewModel(
        string LinkWithoutKey,
        string DecryptionKey,
        string FullLink
    );
    private sealed record UploadResponse(string Token);

    private enum UploadMode { Files, TextSecret }
    private enum SubmitStage { Idle, Encrypting, Uploading, CreatingLink }
    private enum CopiedTarget { None, FullLink, Link, Key }

    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    [Inject]
    private IWebCryptoService CryptoService { get; set; } = null!;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ILogger<Upload> Logger { get; set; } = null!;

    private readonly List<SelectedUploadFile> _selectedFiles = [];
    private readonly CancellationTokenSource _cts = new();

    private UploadMode _mode = UploadMode.Files;
    private SubmitStage _stage = SubmitStage.Idle;
    private UploadResultViewModel? _result;
    private CopiedTarget _copied = CopiedTarget.None;
    private Guid _fileInputKey = Guid.NewGuid();
    private string _textSecret = string.Empty;
    private string _expires = "24h";
    private int _downloads = 1;
    private string? _errorMessage;

    private bool IsBusy => _stage != SubmitStage.Idle;

    private bool IsSubmitDisabled
        => IsBusy
           || _downloads < 1
           || (_mode == UploadMode.Files && _selectedFiles.Count == 0)
           || (_mode == UploadMode.TextSecret && string.IsNullOrWhiteSpace(_textSecret));

    private string SubmitButtonText => _stage switch
    {
        SubmitStage.Encrypting => "Encrypting",
        SubmitStage.Uploading => "Uploading",
        SubmitStage.CreatingLink => "Creating link",
        _ => "Encrypt and upload"
    };

    private void SetMode(UploadMode mode)
    {
        if (IsBusy)
        {
            return;
        }

        _mode = mode;
        _errorMessage = null;
    }

    private string GetModeButtonClass(UploadMode mode)
    {
        const string baseClass = "focus-ring rounded-md px-4 py-2 text-sm font-semibold transition";
        return _mode == mode
            ? $"{baseClass} bg-white text-[#13201d] shadow-sm"
            : $"{baseClass} text-[#52625d] hover:text-[#13201d]";
    }

    private void OnFilesSelected(InputFileChangeEventArgs args)
    {
        if (IsBusy)
        {
            return;
        }

        _errorMessage = null;
        foreach (var file in args.GetMultipleFiles(MaxSelectedFiles))
        {
            _selectedFiles.Add(new SelectedUploadFile(Guid.NewGuid(), file));
        }
    }

    private void RemoveFile(Guid id)
    {
        if (IsBusy)
        {
            return;
        }

        _selectedFiles.RemoveAll(item => item.Id == id);
        ResetFileInput();
    }

    private void ClearFiles()
    {
        if (IsBusy)
        {
            return;
        }

        _selectedFiles.Clear();
        ResetFileInput();
    }

    private async Task EncryptAndUploadAsync()
    {
        if (IsSubmitDisabled)
        {
            return;
        }

        _errorMessage = null;
        _copied = CopiedTarget.None;

        try
        {
            _stage = SubmitStage.Encrypting;
            StateHasChanged();
            await Task.Yield();

            var archiveBytes = _mode == UploadMode.Files
                ? await CreateFilesArchiveAsync(_cts.Token)
                : await CreateTextArchiveAsync(_cts.Token);
            var encrypted = await CryptoService.EncryptAsync(archiveBytes);

            _stage = SubmitStage.Uploading;
            StateHasChanged();

            var token = await UploadEncryptedPayloadAsync(
                encrypted.Payload,
                ResolveContentType(),
                _cts.Token
            );

            _stage = SubmitStage.CreatingLink;
            StateHasChanged();

            _result = CreateResult(token, encrypted.Base64UrlKey);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex) when (
            ex is
                InvalidDataException
                or InvalidOperationException
                or IOException
                or HttpRequestException
                or ArgumentException
                or NotSupportedException
        )
        {
            Logger.LogWarning(ex, "Browser upload failed.");
            _errorMessage = ResolveFriendlyUploadErrorMessage(ex);
        }
        finally
        {
            _stage = SubmitStage.Idle;
            StateHasChanged();
        }
    }

    private async Task<byte[]> CreateFilesArchiveAsync(CancellationToken cancellationToken)
    {
        var totalSizeBytes = _selectedFiles.Sum(item => item.File.Size);
        if (totalSizeBytes > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Total upload size must not exceed 200 MB.");
        }

        await using var stream = new MemoryStream();
        await using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _selectedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryName = CreateUniqueEntryName(item.File.Name, usedNames);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                await using var fileStream = item.File.OpenReadStream(MaxSourceSizeBytes, cancellationToken);
                await using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }

        return stream.ToArray();
    }

    private async Task<byte[]> CreateTextArchiveAsync(CancellationToken cancellationToken)
    {
        var textBytes = Encoding.UTF8.GetBytes(_textSecret);
        if (textBytes.LongLength > MaxSourceSizeBytes)
        {
            throw new InvalidOperationException("Total upload size must not exceed 200 MB.");
        }

        await using var stream = new MemoryStream();
        await using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(ResolveTextEntryName(), CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(textBytes, cancellationToken);
        }

        return stream.ToArray();
    }

    private async Task<string> UploadEncryptedPayloadAsync(
        byte[] encryptedPayload,
        StorageContentType contentType,
        CancellationToken cancellationToken
    )
    {
        using var fileContent = new ByteArrayContent(encryptedPayload);
        using var content = new MultipartFormDataContent
        {
            { fileContent, "file", EncryptedUploadFileName },
            { new StringContent(_expires), "Options.Expires" },
            { new StringContent(Math.Max(_downloads, 1).ToString()), "Options.Downloads" },
            { new StringContent(contentType.ToString()), "Options.ContentType" }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiFilesPath)
        {
            Content = content
        };
        request.Headers.Add(SecShareClientHeaders.ClientType, SecShareClientHeaders.ClientTypeWeb);

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        await SecShareHttpErrorParser.EnsureSuccessResponseAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new InvalidOperationException("Upload response does not contain a file token.");
        }

        return result.Token;
    }

    private StorageContentType ResolveContentType()
        => _mode == UploadMode.TextSecret
            ? StorageContentType.Text
            : StorageContentType.File;

    private UploadResultViewModel CreateResult(string token, string decryptionKey)
    {
        var baseUri = new Uri(Navigation.BaseUri);
        var linkWithoutKey = new Uri(baseUri, $"f/{Uri.EscapeDataString(token)}").ToString();
        var fullLink = $"{linkWithoutKey}#{decryptionKey}";

        return new UploadResultViewModel(linkWithoutKey, decryptionKey, fullLink);
    }

    private async Task CopyFullLinkAsync()
    {
        if (_result is null)
        {
            return;
        }

        await CopyAsync(_result.FullLink, CopiedTarget.FullLink);
    }

    private async Task CopyLinkAsync()
    {
        if (_result is null)
        {
            return;
        }

        await CopyAsync(_result.LinkWithoutKey, CopiedTarget.Link);
    }

    private async Task CopyKeyAsync()
    {
        if (_result is null)
        {
            return;
        }

        await CopyAsync(_result.DecryptionKey, CopiedTarget.Key);
    }

    private async Task CopyAsync(string value, CopiedTarget target)
    {
        try
        {
            await JS.InvokeVoidAsync("secshareInterop.copyText", value);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to copy upload result value.");
        }

        _copied = target;
        StateHasChanged();

        try
        {
            await Task.Delay(2000, _cts.Token);
            if (_copied == target)
            {
                _copied = CopiedTarget.None;
                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CreateAnotherLink()
    {
        _result = null;
        _errorMessage = null;
        _copied = CopiedTarget.None;
        ClearFiles();
        _textSecret = string.Empty;
    }

    private void ResetFileInput()
        => _fileInputKey = Guid.NewGuid();

    private static string ResolveTextEntryName()
        => "secret.txt";

    private static string CreateUniqueEntryName(string fileName, ISet<string> usedNames)
    {
        var entryName = NormalizeEntryName(fileName);
        if (string.IsNullOrWhiteSpace(entryName))
        {
            entryName = "file";
        }

        if (usedNames.Add(entryName))
        {
            return entryName;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(entryName);
        var extension = Path.GetExtension(entryName);
        for (var index = 2; ; index++)
        {
            var candidate = $"{nameWithoutExtension}-{index}{extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string NormalizeEntryName(string value)
    {
        var fileName = Path.GetFileName(value)
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(fileName)
            ? string.Empty
            : fileName;
    }

    private static string ResolveFriendlyUploadErrorMessage(Exception exception)
        => exception switch
        {
            InvalidOperationException ex when ex.Message.Contains("200 MB", StringComparison.OrdinalIgnoreCase) =>
                "Total upload size must not exceed 200 MB.",
            HttpRequestException =>
                "Upload failed. Please check your connection and try again.",
            _ => "Upload failed. Please check the selected content and try again."
        };

    private static string FormatBytes(long bytes)
        => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024):F1} MB"
        };

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}
