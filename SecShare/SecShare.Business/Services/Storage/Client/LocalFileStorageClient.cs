using Microsoft.Extensions.Configuration;

namespace SecShare.Business.Services.Storage.Client;

public class LocalFileStorageClient : IFileStorageClient
{
    private readonly string _rootPath;

    public LocalFileStorageClient(IConfiguration configuration)
    {
        var configuredRootPath = configuration.GetValue<string>("App:Storage:RootPath") ?? "storage";
        _rootPath = Path.IsPathRooted(configuredRootPath)
            ? configuredRootPath
            : Path.Combine(AppContext.BaseDirectory, configuredRootPath);
    }

    public async Task SaveAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var targetStream = File.Create(fullPath);
        await stream.CopyToAsync(targetStream, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(File.OpenRead(GetFullPath(path)));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetFullPath(string path)
    {
        return Path.Combine(_rootPath, path);
    }
}
