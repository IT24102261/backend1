using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class AiWorkflowStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public AgentRole AgentRole { get; set; }
    public string InputSummary { get; set; } = string.Empty;
    public string OutputJson { get; set; } = "{}";
    public string? ToolName { get; set; }
    public string? ToolArgsSummary { get; set; }
    public string? ToolResultSummary { get; set; }
    public string? ValidationJson { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorCode { get; set; }
    public int Attempt { get; set; } = 1;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public AiWorkflow Workflow { get; set; } = null!;
}
