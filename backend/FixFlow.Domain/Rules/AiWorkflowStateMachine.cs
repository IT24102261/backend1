using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Rules;

public static class AiWorkflowStateMachine
{
    private static readonly Dictionary<AiWorkflowStatus, AiWorkflowStatus[]> Allowed = new()
    {
        [AiWorkflowStatus.Created] = [AiWorkflowStatus.Planning, AiWorkflowStatus.Failed, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.Planning] =
        [
            AiWorkflowStatus.ClarificationRequired,
            AiWorkflowStatus.Matching,
            AiWorkflowStatus.Failed,
            AiWorkflowStatus.Cancelled
        ],
        [AiWorkflowStatus.ClarificationRequired] = [AiWorkflowStatus.Planning, AiWorkflowStatus.Cancelled, AiWorkflowStatus.Failed],
        [AiWorkflowStatus.Matching] = [AiWorkflowStatus.QuoteCollection, AiWorkflowStatus.Failed, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.QuoteCollection] = [AiWorkflowStatus.Recommending, AiWorkflowStatus.Validating, AiWorkflowStatus.Failed, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.Recommending] =
        [
            AiWorkflowStatus.WaitingForCustomerApproval,
            AiWorkflowStatus.QuoteCollection,
            AiWorkflowStatus.Failed,
            AiWorkflowStatus.Cancelled
        ],
        [AiWorkflowStatus.WaitingForCustomerApproval] =
        [
            AiWorkflowStatus.Validating,
            AiWorkflowStatus.Recommending,
            AiWorkflowStatus.QuoteCollection,
            AiWorkflowStatus.Cancelled,
            AiWorkflowStatus.Failed
        ],
        [AiWorkflowStatus.Validating] = [AiWorkflowStatus.Completed, AiWorkflowStatus.WaitingForCustomerApproval, AiWorkflowStatus.Failed, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.Completed] = [],
        [AiWorkflowStatus.Failed] = [AiWorkflowStatus.Planning, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.Cancelled] = [],
        [AiWorkflowStatus.Running] = [AiWorkflowStatus.Planning, AiWorkflowStatus.ClarificationRequired, AiWorkflowStatus.Failed, AiWorkflowStatus.Cancelled],
        [AiWorkflowStatus.WaitingApproval] = [AiWorkflowStatus.Validating, AiWorkflowStatus.Cancelled, AiWorkflowStatus.Failed]
    };

    public static AiWorkflowStatus Normalize(AiWorkflowStatus status) => status switch
    {
        AiWorkflowStatus.Running => AiWorkflowStatus.Planning,
        AiWorkflowStatus.WaitingApproval => AiWorkflowStatus.WaitingForCustomerApproval,
        _ => status
    };

    public static bool CanTransition(AiWorkflowStatus from, AiWorkflowStatus to)
    {
        from = Normalize(from);
        to = Normalize(to);
        return from == to || (Allowed.TryGetValue(from, out var next) && next.Contains(to));
    }

    public static bool IsTerminal(AiWorkflowStatus status) =>
        Normalize(status) is AiWorkflowStatus.Completed or AiWorkflowStatus.Cancelled;
}
