using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FixFlow.Infrastructure.Storage;

public class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    public async Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var root = Root();
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var relative = Path.Combine(folder, safeName).Replace('\\', '/');
        var fullPath = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
        return relative;
    }

    public Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(Root());
        var fullPath = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            throw new NotFoundException("File not found.");
        }

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    private string Root() => configuration["Storage:Root"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
}
