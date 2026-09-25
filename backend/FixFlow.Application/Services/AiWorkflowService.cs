using FixFlow.Application.DTOs.Agents;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class AiWorkflowService(
    IRepository<AiWorkflow> workflows,
    IRepository<AiWorkflowStep> steps,
    IRepository<ServiceRequest> requests,
    IAgentOrchestrator orchestrator,
    ICurrentUser currentUser) : IAiWorkflowService
{
    public async Task<WorkflowDto> CreateAsync(CreateWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await requests.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("Request not found.");
        EnsureCanView(entity);
        var result = await orchestrator.StartRequestWorkflowAsync(request.RequestId, cancellationToken);
        return await GetAsync(result.WorkflowId, cancellationToken);
    }

    public async Task<WorkflowDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await Load(id, cancellationToken));

    public async Task<IReadOnlyList<WorkflowStepDto>> StepsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = await Load(id, cancellationToken);
        var items = await steps.Query()
            .Where(x => x.WorkflowId == id)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return items.Select(x => new WorkflowStepDto
        {
            Id = x.Id,
            AgentRole = EnumMap.ToApi(x.AgentRole),
            InputSummary = x.InputSummary,
            OutputJson = x.OutputJson,
            ToolName = x.ToolName,
            ToolResultSummary = x.ToolResultSummary,
            ValidationJson = x.ValidationJson,
            DurationMs = x.DurationMs,
            ErrorCode = x.ErrorCode,
            Attempt = x.Attempt,
            Timestamp = x.Timestamp
        }).ToList();
    }

    public async Task<WorkflowDto> ApproveAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await Load(id, cancellationToken);
        await orchestrator.RecordCustomerDecisionAsync(workflow.RequestId, ApprovalDecision.Approved, request.Reason, cancellationToken);
        if (request.QuotationId is Guid quotationId)
        {
            await orchestrator.ValidateSelectedQuoteAsync(workflow.RequestId, quotationId, currentUser.UserId, cancellationToken);
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<WorkflowDto> RejectAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await Load(id, cancellationToken);
        await orchestrator.RecordCustomerDecisionAsync(workflow.RequestId, ApprovalDecision.Rejected, request.Reason, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<WorkflowDto> ReviseAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = await Load(id, cancellationToken);
        await orchestrator.RecordCustomerDecisionAsync(workflow.RequestId, ApprovalDecision.ChangesRequested, request.Reason, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<AiWorkflow> Load(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Workflow not found.");
        var request = await requests.GetByIdAsync(workflow.RequestId, cancellationToken) ?? throw new NotFoundException("Request not found.");
        EnsureCanView(request);
        return workflow;
    }

    private void EnsureCanView(ServiceRequest request)
    {
        if (currentUser.IsAdmin || request.CustomerId == currentUser.UserId)
        {
            return;
        }

        throw new ForbiddenException();
    }

    private static WorkflowDto Map(AiWorkflow workflow) => new()
    {
        Id = workflow.Id,
        RequestId = workflow.RequestId,
        Objective = workflow.Objective,
        PlanJson = workflow.PlanJson,
        CurrentStep = workflow.CurrentStep,
        Status = EnumMap.ToApi(AiWorkflowStateMachine.Normalize(workflow.Status)),
        ApprovalStatus = EnumMap.ToApi(workflow.ApprovalStatus),
        OutcomeJson = workflow.OutcomeJson,
        CompletedStepsJson = workflow.CompletedStepsJson,
        ErrorCode = workflow.ErrorCode,
        RetryCount = workflow.RetryCount,
        DurationMs = workflow.DurationMs,
        StartedAt = workflow.StartedAt,
        FinishedAt = workflow.FinishedAt
    };
}
