using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.DTOs.Agents;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Interfaces;

public interface IAgentOrchestrator
{
    Task<AgentResponse> RunAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> StartRequestWorkflowAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> ResumeAfterClarificationAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> CollectAndRecommendAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> ValidateSelectedQuoteAsync(Guid requestId, Guid quotationId, Guid customerId, CancellationToken cancellationToken = default);
    Task RecordCustomerDecisionAsync(Guid requestId, ApprovalDecision decision, string? reason, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> CompleteAsync(Guid requestId, object? outcome = null, CancellationToken cancellationToken = default);
    Task<WorkflowRunResult> MarkFailedAsync(Guid requestId, string errorCode, string message, CancellationToken cancellationToken = default);
}
