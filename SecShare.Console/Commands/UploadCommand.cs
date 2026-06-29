using System.ComponentModel;
using System.Text.Json;
using SecShare.Business.Common.Dto.Storage;
using SecShare.Business.Common.Enums;
using SecShare.Business.Exceptions;
using SecShare.Business.Services.Crypto;
using SecShare.Console.Models.Http;
using SecShare.Console.Models.Upload;
using SecShare.Console.Services.Archive;
using SecShare.Console.Services.Http;
using SecShare.Console.Services.Upload;
using SecShare.Console.Ui;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SecShare.Console.Commands;

public sealed class UploadCommand : AsyncCommand<UploadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[path]")]
        [Description("Path to a file or directory to upload.")]
        public string? Path { get; init; }

        [CommandOption("-e|--expires <expires>")]
        [DefaultValue("24h")]
        [Description("Expiration time, for example 24h, 7d, or 30m.")]
        public string Expires { get; init; } = "24h";

        [CommandOption("-d|--downloads <count>")]
        [DefaultValue(1)]
        [Description("Maximum number of downloads.")]
        public int Downloads { get; init; } = 1;

        [CommandOption("--text")]
        [Description("Treat input as plain text instead of a file path.")]
        public bool IsText { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var inputToProcess = settings.Path;
        var isFromStdin = false;

        if (string.IsNullOrWhiteSpace(inputToProcess))
        {
            if (System.Console.IsInputRedirected)
            {
                inputToProcess = await System.Console.In.ReadToEndAsync(cancellationToken);
                isFromStdin = true;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Specify a file or directory path, or pipe text via standard input.");
                return 1;
            }
        }

        AnsiConsole.MarkupLine("[bold]SecShare[/] client-side encrypted upload");
        var displayTarget = isFromStdin ? "Standard Input" : inputToProcess;
        AnsiConsole.MarkupLine($"Target: [cyan]{Markup.Escape(displayTarget)}[/]");
        AnsiConsole.WriteLine();

        var isText = settings.IsText || isFromStdin;
        var contentType = ResolveContentType(inputToProcess, isText);
        UploadPackage package;
        UploadHttpResult uploadResult;
        try
        {
            var archiveService = new ZipArchiveService();
            var uploadPackageService = new UploadPackageService(new CryptoService());
            using var httpClient = new HttpClient
            {
                BaseAddress = SecShareConstants.ServiceBaseUri,
                Timeout = TimeSpan.FromMinutes(5),
            };
            var secShareHttpClient = new SecShareHttpClient(httpClient);

            (package, uploadResult) = await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(TransferProgressUi.CreateColumns())
                .StartAsync(async ctx =>
                {
                    var zipTask = ctx.AddTask("Zipping directory...", autoStart: true, maxValue: 100);
                    var archive = isText
                        ? await archiveService.CreateFromTextAsync(inputToProcess, cancellationToken)
                        : await archiveService.CreateFromPathAsync(inputToProcess, cancellationToken);
                    zipTask.Value = 100;
                    zipTask.StopTask();

                    var encryptTask = ctx.AddTask("Encrypting with AES-256-GCM...", autoStart: true, maxValue: 100);
                    var createdPackage = uploadPackageService.EncryptArchive(archive);
                    encryptTask.Value = 100;
                    encryptTask.StopTask();

                    var uploadTask = ctx.AddTask(
                        $"Uploading to {SecShareConstants.ServiceBaseUri.Host}...",
                        autoStart: true,
                        maxValue: Math.Max(createdPackage.EncryptedPayload.LongLength, 1)
                    );
                    var result = await secShareHttpClient.UploadAsync(
                        createdPackage.EncryptedPayload,
                        new UploadFileOptions
                        {
                            Expires = settings.Expires,
                            Downloads = settings.Downloads,
                            ContentType = contentType
                        },
                        progress => TransferProgressUi.Update(
                            uploadTask,
                            $"Uploading to {SecShareConstants.ServiceBaseUri.Host}...",
                            progress
                        ),
                        cancellationToken
                    );
                    Complete(uploadTask);

                    return (createdPackage, result);
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[red]Upload failed:[/] The request timed out.");
            return 1;
        }
        catch (Exception exception) when (
            exception is
                ArgumentException
                or HttpRequestException
                or IOException
                or JsonException
                or UnauthorizedAccessException
                or InvalidOperationException
                or ApiException
        )
        {
            var errorMessage = ConsoleErrorParser.ResolveFriendlyUploadErrorMessage(exception);
            AnsiConsole.MarkupLine($"[red]Upload failed:[/] {Markup.Escape(errorMessage)}");
            return 1;
        }

        var links = UploadShareLinkBuilder.Create(uploadResult.Token, package.EncryptionKey);
        UploadShareOutputWriter.Write(
            package,
            links,
            contentType.ToString().ToLowerInvariant(),
            settings.Expires,
            settings.Downloads
        );

        return 0;
    }

    private static void Complete(ProgressTask task)
    {
        task.Value = task.MaxValue;
        task.StopTask();
    }

    private static StorageContentType ResolveContentType(string path, bool isText)
    {
        if (isText)
        {
            return StorageContentType.Text;
        }

        return Directory.Exists(path)
            ? StorageContentType.Folder
            : StorageContentType.File;
    }
}
