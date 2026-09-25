namespace FixFlow.Application.Interfaces;

public class AiCompletionRequest
{
    public string AgentName { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string StructuredInputJson { get; set; } = "{}";
    public IReadOnlyList<string> AllowedTools { get; set; } = [];
    public int MaxOutputChars { get; set; } = 8000;
}

public interface IAiModelClient
{
    Task<string> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken = default);
}
