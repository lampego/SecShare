using SecShare.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.SetDefaultCommand<UploadCommand>()
    .WithDescription("Encrypt and upload a file or directory.");

app.Configure(config =>
{
    config.SetApplicationName("secshare");
    config.SetApplicationVersion("0.1.0");

    config.AddCommand<UploadCommand>("upload")
        .WithDescription("Encrypt and upload a file or directory.");

    config.AddCommand<DownloadCommand>("get")
        .WithDescription("Download and decrypt a shared file.");
});

return await app.RunAsync(args);
