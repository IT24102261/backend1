using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Rules;

public static class RequestStateMachine
{
    private static readonly Dictionary<ServiceRequestStatus, ServiceRequestStatus[]> Allowed = new()
    {
        [ServiceRequestStatus.Draft] = [ServiceRequestStatus.Submitted, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Submitted] = [ServiceRequestStatus.Analyzing, ServiceRequestStatus.Cancelled, ServiceRequestStatus.Failed],
        [ServiceRequestStatus.Analyzing] =
        [
            ServiceRequestStatus.ClarificationRequired,
            ServiceRequestStatus.Matching,
            ServiceRequestStatus.CollectingQuotes,
            ServiceRequestStatus.Failed,
            ServiceRequestStatus.Cancelled
        ],
        [ServiceRequestStatus.ClarificationRequired] = [ServiceRequestStatus.Analyzing, ServiceRequestStatus.Submitted, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Matching] = [ServiceRequestStatus.CollectingQuotes, ServiceRequestStatus.Failed, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.CollectingQuotes] = [ServiceRequestStatus.AwaitingCustomerApproval, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.AwaitingCustomerApproval] = [ServiceRequestStatus.Booked, ServiceRequestStatus.CollectingQuotes, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Booked] = [ServiceRequestStatus.Completed, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Completed] = [],
        [ServiceRequestStatus.Cancelled] = [],
        [ServiceRequestStatus.Failed] = []
    };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static bool IsEditable(ServiceRequestStatus status) =>
        status is ServiceRequestStatus.Draft or ServiceRequestStatus.ClarificationRequired;

    public static bool CanCustomerCancel(ServiceRequestStatus status) =>
        status is not (ServiceRequestStatus.Booked or ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled or ServiceRequestStatus.Failed);
}
