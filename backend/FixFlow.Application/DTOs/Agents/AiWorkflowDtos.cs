namespace FixFlow.Application.DTOs.Agents;

public class CreateWorkflowRequest
{
    public Guid RequestId { get; set; }
    public string Objective { get; set; } = string.Empty;
}

public class WorkflowDecisionRequest
{
    public string? Reason { get; set; }
    public Guid? QuotationId { get; set; }
}

public class WorkflowDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string PlanJson { get; set; } = "{}";
    public string CurrentStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? OutcomeJson { get; set; }
    public string CompletedStepsJson { get; set; } = "[]";
    public string? ErrorCode { get; set; }
    public int RetryCount { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}

public class WorkflowStepDto
{
    public Guid Id { get; set; }
    public string AgentRole { get; set; } = string.Empty;
    public string InputSummary { get; set; } = string.Empty;
    public string OutputJson { get; set; } = "{}";
    public string? ToolName { get; set; }
    public string? ToolResultSummary { get; set; }
    public string? ValidationJson { get; set; }
    public string? ErrorCode { get; set; }
    public int Attempt { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
