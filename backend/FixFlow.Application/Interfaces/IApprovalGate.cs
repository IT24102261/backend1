namespace FixFlow.Application.Interfaces;

public interface IApprovalGate
{
    Task<bool> ApproveAsync(string action, string payloadJson, CancellationToken cancellationToken = default);
}
