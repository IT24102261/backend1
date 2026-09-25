namespace FixFlow.Domain.Enums;

public enum AiWorkflowStatus
{
    Created,
    Planning,
    ClarificationRequired,
    Matching,
    QuoteCollection,
    Recommending,
    WaitingForCustomerApproval,
    Validating,
    Completed,
    Failed,
    Cancelled,
    Running,
    WaitingApproval
}
