using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class AiWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string PlanJson { get; set; } = "{}";
    public string CurrentStep { get; set; } = string.Empty;
    public string CompletedStepsJson { get; set; } = "[]";
    public string? OutcomeJson { get; set; }
    public AiWorkflowStatus Status { get; set; } = AiWorkflowStatus.Created;
    public WorkflowApprovalStatus ApprovalStatus { get; set; } = WorkflowApprovalStatus.NotRequired;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string? ErrorCode { get; set; }
    public int RetryCount { get; set; }
    public int DurationMs { get; set; }

    public ServiceRequest Request { get; set; } = null!;
    public ICollection<AiWorkflowStep> Steps { get; set; } = [];
}
