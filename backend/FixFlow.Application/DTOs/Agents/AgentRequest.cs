namespace FixFlow.Application.DTOs.Agents;

public class AgentRequest
{
    public string Goal { get; set; } = string.Empty;
    public string? ContextJson { get; set; }
    public bool RequireHumanApproval { get; set; }
}
