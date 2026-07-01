using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Formatting;
using SecShare.Business.Common.Http;
using SecShare.Business.Common.Services.Archive;
using SecShare.Business.Exceptions;
using SecShare.Web.Services.Crypto;

namespace SecShare.Web.Pages;

public partial class Receive : IAsyncDisposable
{
    private sealed record StageSnapshot(
        PageState State,
        ReadyKind ReadyKind,
        string? KeyError,
        string? ErrorMessage,
        string? TextContent,
        bool IsCopied,
        byte[]? FileBytes,
        string? FileName
    );

    // ── Injected services ─────────────────────────────────────────────────────

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISecShareDownloadClient DownloadClient { get; set; } = null!;

    [Inject]
    private IWebCryptoService CryptoService { get; set; } = null!;

    [Inject]
    private IZipArchiveService ZipArchiveService { get; set; } = null!;

    [Inject]
    private ILogger<Receive> Logger { get; set; } = null!;

    // ── Route parameter ───────────────────────────────────────────────────────

    [Parameter]
    public string Id { get; set; } = string.Empty;

    // ── State machine ─────────────────────────────────────────────────────────

    internal enum PageState { Loading, NeedKey, Decrypting, Ready, Error }
    internal enum ReadyKind { Text, File }

    private PageState _state = PageState.Loading;
    private ReadyKind _readyKind;

    // ── Data ──────────────────────────────────────────────────────────────────

    private string? _encryptionKey;
    private bool _hasKeyFromLink;
    private byte[]? _downloadedData;
    private StorageContentType _contentType = StorageContentType.File;

    // ── UI fields ─────────────────────────────────────────────────────────────

    private string _keyInput = string.Empty;
    private string? _keyError;
    private bool _isDecrypting;
    private string? _errorMessage;

    // Loading progress
    private TransferProgress? _downloadProgress;

    // Ready – text
    private string? _textContent;
    private bool _isCopied;

    // Ready – file / archive
    private byte[]? _fileBytes;
    private string? _fileName;

    private StageSnapshot? _previousStage;
    private readonly CancellationTokenSource _cts = new();

    private bool CanShowBack
        => _state is not PageState.Loading and not PageState.Decrypting;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // Read the decryption key from the URL fragment (it is never sent to the server).
        try
        {
            var hash = await JS.InvokeAsync<string?>("secshareInterop.getLocationHash");
            if (!string.IsNullOrWhiteSpace(hash))
            {
                _encryptionKey = hash;
                _hasKeyFromLink = true;
                // Remove the key from the address bar immediately after reading it.
                await JS.InvokeVoidAsync("secshareInterop.clearLocationHash");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to read or clear decryption key from URL fragment.");
            // JS interop may fail in certain environments; continue without the fragment key.
        }

        await DownloadAsync();
    }

    // ── Download ──────────────────────────────────────────────────────────────

    private async Task DownloadAsync()
    {
        _state = PageState.Loading;
        _downloadProgress = null;
        StateHasChanged();

        try
        {
            var result = await DownloadClient.DownloadAsync(
                Id,
                progress =>
                {
                    _downloadProgress = progress;
                    // InvokeAsync dispatches back to the Blazor synchronisation context.
                    _ = InvokeAsync(StateHasChanged);
                },
                _cts.Token
            );

            _contentType = result.ContentType;
            _downloadedData = result.EncryptedPayload;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Failed to download encrypted payload for file {FileId}.", Id);
            SetError(ex.StatusCode switch
            {
                HttpStatusCode.NotFound => "This link was not found or has already expired.",
                HttpStatusCode.Forbidden or HttpStatusCode.Gone =>
                    "The download limit for this link has been reached.",
                _ => "Failed to load the encrypted content. Please check the link and try again."
            });
            return;
        }
        catch (ApiException ex)
        {
            Logger.LogWarning(ex, "API rejected encrypted payload download for file {FileId}.", Id);
            SetError(ResolveDownloadErrorMessage(ex));
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while downloading encrypted payload for file {FileId}.", Id);
            SetError("Failed to load the encrypted content. Please check your connection and try again.");
            return;
        }

        if (_encryptionKey is not null)
        {
            await DecryptAndProcessAsync();
        }
        else
        {
            _state = PageState.NeedKey;
            StateHasChanged();
        }
    }

    private static string ResolveDownloadErrorMessage(ApiException exception)
    {
        return exception switch
        {
            FileNotFoundDomainException or FileDeletedDomainException =>
                "This link was not found or has already expired.",
            DownloadLimitExhaustedDomainException =>
                "The download limit for this link has been reached.",
            _ when exception.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Gone =>
                "The download limit for this link has been reached.",
            _ when exception.StatusCode == HttpStatusCode.NotFound =>
                "This link was not found or has already expired.",
            _ => "Failed to load the encrypted content. Please check the link and try again."
        };
    }

    // ── Key input ─────────────────────────────────────────────────────────────

    private async Task OnKeyInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnDecryptClicked();
        }
    }

    private async Task OnDecryptClicked()
    {
        _keyError = null;

        var trimmedKey = _keyInput.Trim();
        if (string.IsNullOrEmpty(trimmedKey))
        {
            _keyError = "Please enter the decryption key.";
            return;
        }

        _encryptionKey = trimmedKey;
        _isDecrypting = true;
        StorePreviousStage();
        StateHasChanged();

        await DecryptAndProcessAsync();

        _isDecrypting = false;
        StateHasChanged();
    }

    // ── Decryption ────────────────────────────────────────────────────────────

    private async Task DecryptAndProcessAsync()
    {
        if (_downloadedData is null || _encryptionKey is null)
        {
            return;
        }

        _state = PageState.Decrypting;
        StateHasChanged();

        // Yield to allow Blazor to re-render the "Decrypting…" state before blocking.
        await Task.Yield();

        byte[] decryptedBytes;
        try
        {
            decryptedBytes = await CryptoService.DecryptAsync(_downloadedData, _encryptionKey);
        }
        catch (Exception ex) when (
            IsInvalidDecryptionException(ex)
        )
        {
            Logger.LogWarning(ex, "Failed to decrypt payload for file {FileId}.", Id);
            if (_hasKeyFromLink)
            {
                // Key came from the URL but was invalid; user cannot re-enter it easily.
                SetError(
                    "The decryption key in the link is incorrect. " +
                    "Re-open the original link or ask the sender for a new one."
                );
            }
            else
            {
                _keyError = "The key is incorrect. Please check it and try again.";
                _encryptionKey = null;
                _state = PageState.NeedKey;
                StateHasChanged();
            }

            return;
        }
        catch (Exception ex) when (IsCryptoUnavailableException(ex))
        {
            Logger.LogWarning(ex, "AES-GCM decryption is not available in this browser environment for file {FileId}.", Id);
            SetError(
                "Local AES-GCM decryption is not available in this browser environment. " +
                "Open this link in an up-to-date browser over HTTPS, or use the SecShare CLI."
            );
            return;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while decrypting payload for file {FileId}.", Id);
            SetError("An unexpected error occurred during decryption.");
            return;
        }

        await ProcessZipAsync(decryptedBytes);
    }

    private static bool IsInvalidDecryptionException(Exception exception)
        => exception is
               CryptographicException
               or FormatException
               or ArgumentException
           || exception.InnerException is not null
           && IsInvalidDecryptionException(exception.InnerException);

    private static bool IsCryptoUnavailableException(Exception exception)
        => exception is
               PlatformNotSupportedException
               or NotSupportedException
           || exception.InnerException is not null
           && IsCryptoUnavailableException(exception.InnerException);

    // ── ZIP processing ────────────────────────────────────────────────────────

    private async Task ProcessZipAsync(byte[] zipBytes)
    {
        try
        {
            var content = await ZipArchiveService.ReadContentAsync(
                zipBytes,
                _contentType,
                _cts.Token
            );
            if (content.IsText)
            {
                _textContent = content.TextContent;
                _readyKind = ReadyKind.Text;
                _state = PageState.Ready;
                StateHasChanged();
                return;
            }

            _fileBytes = content.FileBytes
                ?? throw new InvalidDataException("ZIP archive did not produce downloadable file content.");
            _fileName = content.FileName
                ?? throw new InvalidDataException("ZIP archive did not produce a downloadable file name.");
            _readyKind = ReadyKind.File;
            _state = PageState.Ready;
            StateHasChanged();

            // Trigger the browser's save dialog automatically.
            await SaveFileAsync();
        }
        catch (InvalidDataException ex)
        {
            Logger.LogWarning(ex, "Decrypted payload is not a valid ZIP archive for file {FileId}.", Id);
            SetError(
                "The decrypted data appears to be corrupted or in an unsupported format. " +
                "Verify that the correct key was used."
            );
        }
        catch (OperationCanceledException)
        {
            // Cancelled — no state change needed.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while processing decrypted ZIP content for file {FileId}.", Id);
            SetError("An error occurred while processing the decrypted content.");
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private async Task SaveFileAsync()
    {
        if (_fileBytes is null || _fileName is null)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("secshareInterop.saveFile", _fileName, _fileBytes);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save decrypted file {FileName} through JS interop.", _fileName);
            // JS interop failure is non-critical; the "Download again" button remains available.
        }
    }

    private async Task CopyTextAsync()
    {
        if (_textContent is null)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("secshareInterop.copyText", _textContent);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to copy decrypted text to clipboard.");
            // Clipboard access may be denied; ignore silently.
        }

        _isCopied = true;
        StateHasChanged();

        await Task.Delay(2000, _cts.Token);

        _isCopied = false;
        StateHasChanged();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnBackClicked()
    {
        if (_previousStage is null || _previousStage.State == _state)
        {
            Navigation.NavigateTo("/receive");
            return;
        }

        var stage = _previousStage;
        _previousStage = null;

        _state = stage.State;
        _readyKind = stage.ReadyKind;
        _keyError = stage.KeyError;
        _errorMessage = stage.ErrorMessage;
        _textContent = stage.TextContent;
        _isCopied = stage.IsCopied;
        _fileBytes = stage.FileBytes;
        _fileName = stage.FileName;
        _isDecrypting = false;

        StateHasChanged();
    }

    private void StorePreviousStage()
    {
        if (_state is PageState.Loading or PageState.Decrypting)
        {
            return;
        }

        _previousStage = new StageSnapshot(
            _state,
            _readyKind,
            _keyError,
            _errorMessage,
            _textContent,
            _isCopied,
            _fileBytes,
            _fileName
        );
    }

    private void SetError(string message)
    {
        _errorMessage = message;
        _state = PageState.Error;
        StateHasChanged();
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        return $"{ByteSizeFormatter.Format(bytesPerSecond)}/s";
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}
