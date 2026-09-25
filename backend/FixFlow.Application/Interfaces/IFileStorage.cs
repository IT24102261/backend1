namespace FixFlow.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default);
}
