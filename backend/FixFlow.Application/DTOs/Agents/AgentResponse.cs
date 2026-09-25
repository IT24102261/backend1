namespace FixFlow.Application.DTOs.Agents;

public class AgentResponse
{
    public bool Approved { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string OutputJson { get; set; } = "{}";
    public IReadOnlyList<string> ToolsUsed { get; set; } = [];
}
