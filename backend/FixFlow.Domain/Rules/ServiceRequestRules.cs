using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Rules;

public static class ServiceRequestRules
{
    public static bool RequiresApproval(ServiceRequestStatus status) =>
        status is ServiceRequestStatus.AwaitingCustomerApproval or ServiceRequestStatus.Booked;
}
