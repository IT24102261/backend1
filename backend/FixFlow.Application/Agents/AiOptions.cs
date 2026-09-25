namespace FixFlow.Application.Agents;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Deterministic";
    public int MaxPromptChars { get; set; } = 4000;
    public int MaxOutputChars { get; set; } = 8000;
    public int MaxRetries { get; set; } = 2;
    public int ToolTimeoutSeconds { get; set; } = 8;
}
