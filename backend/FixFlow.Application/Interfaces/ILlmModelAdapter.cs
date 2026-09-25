namespace FixFlow.Application.Interfaces;

public interface ILlmModelAdapter
{
    Task<string> CompleteJsonAsync(string prompt, CancellationToken cancellationToken = default);
}
