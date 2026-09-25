namespace FixFlow.Domain.Enums;

public enum ServiceRequestStatus
{
    Draft,
    Submitted,
    Analyzing,
    ClarificationRequired,
    Matching,
    CollectingQuotes,
    AwaitingCustomerApproval,
    Booked,
    Completed,
    Cancelled,
    Failed
}
