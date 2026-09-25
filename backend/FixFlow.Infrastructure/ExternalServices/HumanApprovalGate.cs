using FixFlow.Application.Interfaces;

namespace FixFlow.Infrastructure.ExternalServices;

public class HumanApprovalGate : IApprovalGate
{
    public Task<bool> ApproveAsync(string action, string payloadJson, CancellationToken cancellationToken = default)
    {
        _ = action;
        _ = payloadJson;
        return Task.FromResult(false);
    }
}
