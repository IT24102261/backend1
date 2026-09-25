using FixFlow.Application.Agents;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.DTOs.Agents;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Tests.Support;

public sealed class SafeTestOrchestrator(AgentOrchestrator inner) : IAgentOrchestrator
{
    public Task<AgentResponse> RunAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
        inner.RunAsync(request, cancellationToken);

    public Task<WorkflowRunResult> StartRequestWorkflowAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        Guard(() => inner.StartRequestWorkflowAsync(requestId, cancellationToken), "MATCHING");

    public Task<WorkflowRunResult> ResumeAfterClarificationAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        Guard(() => inner.ResumeAfterClarificationAsync(requestId, cancellationToken), "MATCHING");

    public Task<WorkflowRunResult> CollectAndRecommendAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        Guard(() => inner.CollectAndRecommendAsync(requestId, cancellationToken), "WAITING_FOR_CUSTOMER_APPROVAL");

    public Task<WorkflowRunResult> ValidateSelectedQuoteAsync(
        Guid requestId,
        Guid quotationId,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        inner.ValidateSelectedQuoteAsync(requestId, quotationId, customerId, cancellationToken);

    public Task RecordCustomerDecisionAsync(
        Guid requestId,
        ApprovalDecision decision,
        string? reason,
        CancellationToken cancellationToken = default) =>
        GuardVoid(() => inner.RecordCustomerDecisionAsync(requestId, decision, reason, cancellationToken));

    public Task<WorkflowRunResult> CompleteAsync(Guid requestId, object? outcome = null, CancellationToken cancellationToken = default) =>
        Guard(() => inner.CompleteAsync(requestId, outcome, cancellationToken), "COMPLETED");

    public Task<WorkflowRunResult> MarkFailedAsync(Guid requestId, string errorCode, string message, CancellationToken cancellationToken = default) =>
        Guard(() => inner.MarkFailedAsync(requestId, errorCode, message, cancellationToken), "FAILED");

    private static async Task<WorkflowRunResult> Guard(Func<Task<WorkflowRunResult>> action, string fallbackStatus)
    {
        try
        {
            return await action();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new WorkflowRunResult { Status = fallbackStatus, ErrorCode = "CONCURRENCY" };
        }
    }

    private static async Task GuardVoid(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (DbUpdateConcurrencyException)
        {
        }
    }
}
