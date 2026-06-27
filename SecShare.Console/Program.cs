using SecShare.Console.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.SetDefaultCommand<UploadCommand>()
    .WithDescription("Encrypt and upload a file or directory, then print a full secure link and separate link/decryption key.");

app.Configure(config =>
{
    config.SetApplicationName("secshare");
    config.SetApplicationVersion("0.1.0");

    config.AddCommand<UploadCommand>("upload")
        .WithDescription("Encrypt and upload a file or directory, then print a full secure link and separate link/decryption key.")
        .WithExample(["upload", "./report.pdf"]);

    config.AddCommand<DownloadCommand>("get")
        .WithDescription("Download and decrypt a shared file. Pass a full link with key, or a link without key and SecShare will ask for it.")
        .WithExample(["get", "https://secshare.me/f/019f0969-3c88-7ab7-96fc-d94d82c41c40#KEY"])
        .WithExample(["get", "https://secshare.me/f/019f0969-3c88-7ab7-96fc-d94d82c41c40"]);
});

return await app.RunAsync(args);
