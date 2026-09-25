namespace FixFlow.Application.Interfaces;

public interface IToolCaller
{
    Task<string> CallAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
