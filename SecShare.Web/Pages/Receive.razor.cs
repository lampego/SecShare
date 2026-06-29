using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SecShare.Business.Common.Crypto;
using SecShare.Business.Common.Enums;
using SecShare.Business.Common.Headers;
using SecShare.Business.Common.Http;
using SecShare.Business.Exceptions;

namespace SecShare.Web.Pages;

public partial class Receive : IAsyncDisposable
{
    // ── Injected services ─────────────────────────────────────────────────────

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private ISecShareDownloadClient DownloadClient { get; set; } = null!;

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

    private readonly CancellationTokenSource _cts = new();

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
        catch
        {
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
            SetError(ResolveDownloadErrorMessage(ex));
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
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
            var crypto = new CryptoService();
            decryptedBytes = crypto.Decrypt(_downloadedData, _encryptionKey);
        }
        catch (Exception ex) when (
            ex is CryptographicException
                or FormatException
                or ArgumentException
        )
        {
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
        catch
        {
            SetError("An unexpected error occurred during decryption.");
            return;
        }

        await ProcessZipAsync(decryptedBytes);
    }

    // ── ZIP processing ────────────────────────────────────────────────────────

    private async Task ProcessZipAsync(byte[] zipBytes)
    {
        try
        {
            await using var stream = new MemoryStream(zipBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (_contentType == StorageContentType.Text)
            {
                await ProcessTextArchiveAsync(archive);
                return;
            }

            var fileEntries = archive.Entries
                .Where(e => !IsDirectoryEntry(e))
                .ToArray();

            if (_contentType == StorageContentType.File && fileEntries.Length == 1)
            {
                // Single file — extract the inner file instead of serving the ZIP wrapper.
                var entry = fileEntries[0];
                var entryName = Path.GetFileName(entry.FullName);
                if (string.IsNullOrEmpty(entryName))
                {
                    entryName = "secshare-file";
                }

                await using var ms = new MemoryStream();
                await using (var entryStream = entry.Open())
                {
                    await entryStream.CopyToAsync(ms, _cts.Token);
                }

                _fileBytes = ms.ToArray();
                _fileName = entryName;
            }
            else
            {
                // Multiple files or a directory tree — keep the ZIP.
                _fileBytes = zipBytes;
                _fileName = ResolveArchiveFileName(archive);
            }

            _readyKind = ReadyKind.File;
            _state = PageState.Ready;
            StateHasChanged();

            // Trigger the browser's save dialog automatically.
            await SaveFileAsync();
        }
        catch (InvalidDataException)
        {
            SetError(
                "The decrypted data appears to be corrupted or in an unsupported format. " +
                "Verify that the correct key was used."
            );
        }
        catch (OperationCanceledException)
        {
            // Cancelled — no state change needed.
        }
        catch
        {
            SetError("An error occurred while processing the decrypted content.");
        }
    }

    private async Task ProcessTextArchiveAsync(ZipArchive archive)
    {
        var textEntry = archive.Entries.FirstOrDefault(e => !IsDirectoryEntry(e));
        if (textEntry is null)
        {
            SetError("The decrypted content could not be read as text.");
            return;
        }

        using var reader = new StreamReader(
            textEntry.Open(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true
        );
        _textContent = await reader.ReadToEndAsync(_cts.Token);
        _readyKind = ReadyKind.Text;
        _state = PageState.Ready;
        StateHasChanged();
    }

    /// <summary>
    /// Derives a ZIP archive filename from the root directory entry embedded in the ZIP,
    /// falling back to a safe default name.
    /// </summary>
    private string ResolveArchiveFileName(ZipArchive archive)
    {
        if (_contentType == StorageContentType.Folder)
        {
            // The ZIP was created with the directory name as the top-level entry.
            var rootDir = archive.Entries
                .Where(e => IsDirectoryEntry(e))
                .Select(e => e.FullName.TrimEnd('/'))
                .FirstOrDefault(name => !name.Contains('/'));

            if (!string.IsNullOrWhiteSpace(rootDir))
            {
                return $"{rootDir}.zip";
            }
        }

        return "secshare-files.zip";
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
        catch
        {
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
        catch
        {
            // Clipboard access may be denied; ignore silently.
        }

        _isCopied = true;
        StateHasChanged();

        await Task.Delay(2000, _cts.Token);

        _isCopied = false;
        StateHasChanged();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetError(string message)
    {
        _errorMessage = message;
        _state = PageState.Error;
        StateHasChanged();
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        => entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024):F1} MB"
        };
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024) return $"{bytesPerSecond:F0} B/s";
        if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F1} KB/s";
        return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}
