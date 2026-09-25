using System.Text.Json;
using System.Text.Json.Nodes;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Agents;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixFlow.Application.Agents;

public class AgentOrchestrator(
    IAiModelClient model,
    RequestPlanningAgent planningAgent,
    TechnicianMatchingAgent matchingAgent,
    QuotationRecommendationAgent recommendationAgent,
    BookingValidationAgent validationAgent,
    IRepository<ServiceRequest> requests,
    IRepository<RequestMedia> media,
    IRepository<RequestClarification> clarifications,
    IRepository<RequestInvitation> invitations,
    IRepository<ServiceCategory> categories,
    IRepository<AiWorkflow> workflows,
    IRepository<AiWorkflowStep> steps,
    IRepository<Approval> approvals,
    IRepository<AuditLog> audits,
    IRepository<RequestStatusHistory> requestHistory,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<AiOptions> options,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    public async Task<AgentResponse> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ContextJson))
        {
            using var document = JsonDocument.Parse(request.ContextJson);
            if (document.RootElement.TryGetProperty("requestId", out var idEl)
                && Guid.TryParse(idEl.ToString(), out var requestId))
            {
                var run = await StartRequestWorkflowAsync(requestId, cancellationToken);
                return new AgentResponse
                {
                    Approved = run.Status == "WAITING_FOR_CUSTOMER_APPROVAL" || run.Status == "COMPLETED",
                    Summary = run.CurrentStep,
                    OutputJson = run.PlanJson,
                    ToolsUsed = ["orchestrator"]
                };
            }
        }

        var raw = await model.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = "Compatibility",
            Objective = AgentSafety.SanitizeUserText(request.Goal, 500),
            StructuredInputJson = request.ContextJson ?? "{}",
            MaxOutputChars = options.Value.MaxOutputChars
        }, cancellationToken);

        return new AgentResponse
        {
            Approved = !request.RequireHumanApproval,
            Summary = "Structured compatibility response.",
            OutputJson = raw,
            ToolsUsed = []
        };
    }

    public Task<WorkflowRunResult> StartRequestWorkflowAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        RunPlanningAndMatchingAsync(requestId, resume: false, cancellationToken);

    public Task<WorkflowRunResult> ResumeAfterClarificationAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        RunPlanningAndMatchingAsync(requestId, resume: true, cancellationToken);

    public async Task<WorkflowRunResult> CollectAndRecommendAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await LoadRequest(requestId, cancellationToken);
        var workflow = await GetOrCreateQuoteCollectionWorkflow(request, cancellationToken);
        if (AiWorkflowStateMachine.IsTerminal(workflow.Status))
        {
            return Map(workflow);
        }

        try
        {
            await Move(workflow, AiWorkflowStatus.Recommending, "RECOMMENDING", cancellationToken);
            var output = await RunWithRetry(async () =>
            {
                var context = Context(workflow, requestId, AgentRole.QuotationRecommendation);
                return await recommendationAgent.RunAsync(new RecommendationInput { RequestId = requestId }, context, options.Value.MaxOutputChars, cancellationToken);
            }, workflow, AgentRole.QuotationRecommendation, cancellationToken);

            MergePlan(workflow, "recommendation", output.Output);
            await PersistWorkflowAsync(cancellationToken);
            await RecordStep(workflow, AgentRole.QuotationRecommendation, "Compare real quotations", output.Output, output.ToolsUsed, null, cancellationToken);

            if (output.ValidQuoteIds.Count == 0)
            {
                await Move(workflow, AiWorkflowStatus.QuoteCollection, "QUOTE_COLLECTION", cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Map(workflow);
            }

            await Move(workflow, AiWorkflowStatus.WaitingForCustomerApproval, "WAITING_FOR_CUSTOMER_APPROVAL", cancellationToken);
            workflow.ApprovalStatus = WorkflowApprovalStatus.Pending;
            await PersistWorkflowAsync(cancellationToken);
            await ChangeRequestStatusAsync(request, ServiceRequestStatus.AwaitingCustomerApproval, "Quotations ready. Waiting for customer approval.", cancellationToken);
            return Map(workflow);
        }
        catch (Exception ex) when (ex is AgentOutputException or OperationCanceledException)
        {
            return await Fail(workflow, request, ex, cancellationToken);
        }
    }

    public async Task<WorkflowRunResult> ValidateSelectedQuoteAsync(
        Guid requestId,
        Guid quotationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadRequest(requestId, cancellationToken);
        var workflow = await RequireWorkflow(requestId, cancellationToken);
        try
        {
            await Move(workflow, AiWorkflowStatus.Validating, "VALIDATING", cancellationToken);
            var output = await RunWithRetry(async () =>
            {
                var context = Context(workflow, requestId, AgentRole.BookingValidation);
                return await validationAgent.RunAsync(
                    new ValidationInput
                    {
                        RequestId = requestId,
                        QuotationId = quotationId,
                        CustomerId = customerId,
                        WorkflowId = workflow.Id
                    },
                    context,
                    options.Value.MaxOutputChars,
                    cancellationToken);
            }, workflow, AgentRole.BookingValidation, cancellationToken);

            MergePlan(workflow, "validation", output.Output);
            await PersistWorkflowAsync(cancellationToken);
            await RecordStep(
                workflow,
                AgentRole.BookingValidation,
                $"Validate quotation {quotationId}",
                output.Output,
                output.ToolsUsed,
                new { output.Output.Valid, output.Output.BookingAllowed, output.Output.Errors },
                cancellationToken);

            if (!output.Output.BookingAllowed)
            {
                workflow.ErrorCode = "BOOKING_REJECTED";
                await Move(workflow, AiWorkflowStatus.WaitingForCustomerApproval, "WAITING_FOR_CUSTOMER_APPROVAL", cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                var rejected = Map(workflow);
                rejected.Validation = output.Output;
                return rejected;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            var allowed = Map(workflow);
            allowed.Validation = output.Output;
            return allowed;
        }
        catch (Exception ex) when (ex is AgentOutputException or OperationCanceledException)
        {
            return await Fail(workflow, request, ex, cancellationToken);
        }
    }

    public async Task RecordCustomerDecisionAsync(
        Guid requestId,
        ApprovalDecision decision,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadRequest(requestId, cancellationToken);
        if (!currentUser.IsAdmin && request.CustomerId != currentUser.UserId)
        {
            throw new Exceptions.ForbiddenException("Only the owning customer can approve this workflow.");
        }

        var workflow = await RequireWorkflow(requestId, cancellationToken);
        await approvals.AddAsync(new Approval
        {
            WorkflowId = workflow.Id,
            RequestId = requestId,
            ActorId = currentUser.UserId == Guid.Empty ? request.CustomerId : currentUser.UserId,
            Decision = decision,
            Reason = AgentSafety.SanitizeUserText(reason, 2000)
        }, cancellationToken);

        workflow.ApprovalStatus = decision switch
        {
            ApprovalDecision.Approved => WorkflowApprovalStatus.Approved,
            ApprovalDecision.Rejected => WorkflowApprovalStatus.Rejected,
            _ => WorkflowApprovalStatus.Pending
        };

        if (decision == ApprovalDecision.Rejected)
        {
            await Move(workflow, AiWorkflowStatus.Cancelled, "CANCELLED", cancellationToken);
            workflow.FinishedAt = DateTimeOffset.UtcNow;
            await PersistWorkflowAsync(cancellationToken);
            await ChangeRequestStatusAsync(request, ServiceRequestStatus.Cancelled, "Customer rejected the booking recommendation.", cancellationToken);
        }
        else if (decision == ApprovalDecision.ChangesRequested)
        {
            await Move(workflow, AiWorkflowStatus.QuoteCollection, "QUOTE_COLLECTION", cancellationToken);
            await PersistWorkflowAsync(cancellationToken);
            await ChangeRequestStatusAsync(request, ServiceRequestStatus.CollectingQuotes, "Customer requested a revision.", cancellationToken);
        }
        else
        {
            await PersistWorkflowAsync(cancellationToken);
        }

        await Audit("CUSTOMER_APPROVAL", workflow.Id, AuditOutcome.Success, cancellationToken, new { decision = EnumMap.ToApi(decision) });
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowRunResult> CompleteAsync(Guid requestId, object? outcome = null, CancellationToken cancellationToken = default)
    {
        var workflow = await RequireWorkflow(requestId, cancellationToken);
        workflow.OutcomeJson = outcome is null ? workflow.OutcomeJson : JsonSerializer.Serialize(outcome, AgentJson.Options);
        workflow.ApprovalStatus = WorkflowApprovalStatus.Approved;
        await Move(workflow, AiWorkflowStatus.Completed, "COMPLETED", cancellationToken);
        workflow.FinishedAt = DateTimeOffset.UtcNow;
        workflow.DurationMs = (int)(workflow.FinishedAt.Value - workflow.StartedAt).TotalMilliseconds;
        await Audit("WORKFLOW_COMPLETED", workflow.Id, AuditOutcome.Success, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(workflow);
    }

    public async Task<WorkflowRunResult> MarkFailedAsync(Guid requestId, string errorCode, string message, CancellationToken cancellationToken = default)
    {
        var request = await LoadRequest(requestId, cancellationToken);
        var workflow = await RequireWorkflow(requestId, cancellationToken);
        return await Fail(workflow, request, new AgentOutputException(errorCode, message), cancellationToken);
    }

    private async Task<WorkflowRunResult> RunPlanningAndMatchingAsync(Guid requestId, bool resume, CancellationToken cancellationToken)
    {
        var request = await LoadRequest(requestId, cancellationToken);
        var workflow = await GetOrCreateWorkflow(request, cancellationToken);
        try
        {
            var current = AiWorkflowStateMachine.Normalize(workflow.Status);
            var recordedCount = (await steps.Query().Where(x => x.WorkflowId == workflow.Id).ToMaterializedListAsync(cancellationToken)).Count;
            if (AiWorkflowStateMachine.IsTerminal(current))
            {
                return Map(workflow);
            }

            if (recordedCount > 0 && current is AiWorkflowStatus.QuoteCollection or AiWorkflowStatus.Recommending
                or AiWorkflowStatus.WaitingForCustomerApproval or AiWorkflowStatus.Validating)
            {
                return Map(workflow);
            }

            if (recordedCount == 0 && current is not AiWorkflowStatus.Created)
            {
                workflow.Status = AiWorkflowStatus.Planning;
                workflow.CurrentStep = "PLANNING";
                await PersistWorkflowAsync(cancellationToken);
                current = AiWorkflowStatus.Planning;
            }

            if (resume || current is not AiWorkflowStatus.Created)
            {
                if (current is AiWorkflowStatus.Created or AiWorkflowStatus.ClarificationRequired or AiWorkflowStatus.Failed)
                {
                    await Move(workflow, AiWorkflowStatus.Planning, "PLANNING", cancellationToken);
                }
            }
            else
            {
                await Move(workflow, AiWorkflowStatus.Created, "CREATED", cancellationToken);
                await Move(workflow, AiWorkflowStatus.Planning, "PLANNING", cancellationToken);
            }

            var images = await media.Query().Where(x => x.RequestId == requestId).ToMaterializedListAsync(cancellationToken);
            var notes = await clarifications.Query().Where(x => x.RequestId == requestId).ToMaterializedListAsync(cancellationToken);
            var description = string.Join("\n", new[] { request.Description }.Concat(notes.Select(x => x.Message)));
            if (AgentSafety.IsPrimarilyInjection(description))
            {
                throw new AgentOutputException("PROMPT_INJECTION", "Prompt-injection content was rejected before tool execution.");
            }

            var optionalCategory = request.Category?.Name;
            if (optionalCategory is null && request.CategoryId is Guid categoryId)
            {
                optionalCategory = (await categories.GetByIdAsync(categoryId, cancellationToken))?.Name;
            }

            var planning = await RunWithRetry(async () =>
            {
                var context = Context(workflow, requestId, AgentRole.RequestPlanning);
                return await planningAgent.RunAsync(
                    new PlanningInput
                    {
                        RequestId = requestId,
                        Description = description,
                        OptionalCategory = optionalCategory,
                        ServiceArea = request.ServiceArea,
                        PreferredStart = request.PreferredStart,
                        PreferredEnd = request.PreferredEnd,
                        Images = images.Select(x => new ImageMetadata { Id = x.Id, MimeType = x.MimeType, UploadedAt = x.UploadedAt }).ToList()
                    },
                    context,
                    options.Value.MaxOutputChars,
                    cancellationToken);
            }, workflow, AgentRole.RequestPlanning, cancellationToken);

            MergePlan(workflow, "planning", planning.Output);
            await PersistWorkflowAsync(cancellationToken);
            await RecordStep(workflow, AgentRole.RequestPlanning, "Classify request", planning.Output, planning.ToolsUsed, new { planning.Output.Category, planning.Output.ClarificationRequired }, cancellationToken);

            var catalogItem = (await categories.Query().ToMaterializedListAsync(cancellationToken))
                .FirstOrDefault(x => x.IsActive && x.Name.Equals(planning.Output.Category, StringComparison.OrdinalIgnoreCase));
            if (catalogItem is not null && request.CategoryId != catalogItem.Id)
            {
                request.CategoryId = catalogItem.Id;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (planning.Output.ClarificationRequired || catalogItem is null)
            {
                await Move(workflow, AiWorkflowStatus.ClarificationRequired, "CLARIFICATION_REQUIRED", cancellationToken);
                await PersistWorkflowAsync(cancellationToken);
                await ChangeRequestStatusAsync(request, ServiceRequestStatus.ClarificationRequired, string.Join(" ", planning.Output.ClarificationQuestions), cancellationToken);
                var paused = Map(workflow);
                paused.ClarificationRequired = true;
                return paused;
            }

            await Move(workflow, AiWorkflowStatus.Matching, "MATCHING", cancellationToken);
            var matching = await RunWithRetry(async () =>
            {
                var context = Context(workflow, requestId, AgentRole.TechnicianMatching);
                return await matchingAgent.RunAsync(
                    new MatchingInput
                    {
                        RequestId = requestId,
                        CategoryId = request.CategoryId,
                        ServiceArea = request.ServiceArea,
                        PreferredStart = request.PreferredStart,
                        PreferredEnd = request.PreferredEnd
                    },
                    context,
                    options.Value.MaxOutputChars,
                    cancellationToken);
            }, workflow, AgentRole.TechnicianMatching, cancellationToken);

            var eligibleIds = matching.Output.EligibleTechnicians.Select(x => x.TechnicianId).Distinct().ToList();
            MergePlan(workflow, "matching", matching.Output);
            await PersistWorkflowAsync(cancellationToken);
            await RecordStep(workflow, AgentRole.TechnicianMatching, "Match approved technicians", matching.Output, matching.ToolsUsed, new { eligible = eligibleIds.Count }, cancellationToken);

            var existing = await invitations.Query().Where(x => x.RequestId == requestId).ToMaterializedListAsync(cancellationToken);
            var created = eligibleIds.Except(existing.Select(x => x.TechnicianId))
                .Select(technicianId => new RequestInvitation { RequestId = requestId, TechnicianId = technicianId })
                .ToList();
            if (created.Count > 0)
            {
                await invitations.AddRangeAsync(created, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await Move(workflow, AiWorkflowStatus.QuoteCollection, "QUOTE_COLLECTION", cancellationToken);
            await ChangeRequestStatusAsync(
                request,
                created.Count > 0 ? ServiceRequestStatus.CollectingQuotes : ServiceRequestStatus.Matching,
                created.Count > 0 ? $"Invited {created.Count} category-approved technicians." : "No eligible verified technicians yet.",
                cancellationToken);
            return Map(workflow);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Workflow persist collided for request {RequestId}", requestId);
            return Map(workflow);
        }
        catch (Exception ex) when (ex is AgentOutputException or OperationCanceledException)
        {
            return await Fail(workflow, request, ex, cancellationToken);
        }
    }

    private async Task<T> RunWithRetry<T>(Func<Task<T>> action, AiWorkflow workflow, AgentRole agent, CancellationToken cancellationToken)
    {
        AgentOutputException? last = null;
        var attempts = Math.Max(1, options.Value.MaxRetries + 1);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (AgentOutputException ex) when (ex.Code is "MALFORMED_AI_RESPONSE" or "TOOL_TIMEOUT" && attempt < attempts)
            {
                last = ex;
                workflow.RetryCount++;
                await PersistWorkflowAsync(cancellationToken);
                await RecordStep(workflow, agent, $"Retry {attempt}: {ex.Code}", new { ex.Code, ex.Message }, [], new { retry = true }, cancellationToken, ex.Code, attempt);
            }
        }

        throw last ?? new AgentOutputException("FAILED", "The agent could not complete safely.");
    }

    private async Task<AiWorkflow> GetOrCreateWorkflow(ServiceRequest request, CancellationToken cancellationToken)
    {
        var existing = (await workflows.Query().Where(x => x.RequestId == request.Id).ToMaterializedListAsync(cancellationToken))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault();
        if (existing is not null)
        {
            return existing;
        }

        var workflow = new AiWorkflow
        {
            RequestId = request.Id,
            Objective = "Plan, match, recommend, and validate a home-service booking.",
            Status = AiWorkflowStatus.Created,
            CurrentStep = "CREATED",
            PlanJson = "{}",
            CompletedStepsJson = "[]"
        };
        await workflows.AddAsync(workflow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return workflow;
    }

    private async Task<AiWorkflow> GetOrCreateQuoteCollectionWorkflow(ServiceRequest request, CancellationToken cancellationToken)
    {
        var existing = (await workflows.Query().Where(x => x.RequestId == request.Id).ToMaterializedListAsync(cancellationToken))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault();
        if (existing is null)
        {
            var created = new AiWorkflow
            {
                RequestId = request.Id,
                Objective = "Plan, match, recommend, and validate a home-service booking.",
                Status = AiWorkflowStatus.QuoteCollection,
                CurrentStep = "QUOTE_COLLECTION",
                PlanJson = "{}",
                CompletedStepsJson = "[\"QUOTE_COLLECTION\"]"
            };
            await workflows.AddAsync(created, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return created;
        }

        var status = AiWorkflowStateMachine.Normalize(existing.Status);
        if (status is AiWorkflowStatus.Created or AiWorkflowStatus.Planning or AiWorkflowStatus.Matching or AiWorkflowStatus.ClarificationRequired)
        {
            if (AiWorkflowStateMachine.CanTransition(status, AiWorkflowStatus.QuoteCollection))
            {
                await Move(existing, AiWorkflowStatus.QuoteCollection, "QUOTE_COLLECTION", cancellationToken);
            }
            else
            {
                existing.Status = AiWorkflowStatus.QuoteCollection;
                existing.CurrentStep = "QUOTE_COLLECTION";
            }

            await PersistWorkflowAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<AiWorkflow> RequireWorkflow(Guid requestId, CancellationToken cancellationToken) =>
        (await workflows.Query().Where(x => x.RequestId == requestId).ToMaterializedListAsync(cancellationToken))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault()
        ?? throw new AgentOutputException("NOT_FOUND", "Workflow not found.");

    private async Task<ServiceRequest> LoadRequest(Guid requestId, CancellationToken cancellationToken) =>
        await requests.GetByIdAsync(requestId, cancellationToken)
        ?? throw new AgentOutputException("NOT_FOUND", "Request not found.");

    private async Task Move(AiWorkflow workflow, AiWorkflowStatus next, string step, CancellationToken cancellationToken)
    {
        var current = AiWorkflowStateMachine.Normalize(workflow.Status);
        next = AiWorkflowStateMachine.Normalize(next);
        if (!AiWorkflowStateMachine.CanTransition(current, next))
        {
            throw new AgentOutputException("INVALID_STATE", $"Cannot move workflow from {current} to {next}.");
        }

        workflow.Status = next;
        workflow.CurrentStep = step;
        var completed = JsonNode.Parse(string.IsNullOrWhiteSpace(workflow.CompletedStepsJson) ? "[]" : workflow.CompletedStepsJson)?.AsArray() ?? [];
        if (completed.All(x => x?.ToString() != step))
        {
            completed.Add(step);
        }

        workflow.CompletedStepsJson = completed.ToJsonString();
        await PersistWorkflowAsync(cancellationToken);
        await Audit("WORKFLOW_STEP", workflow.Id, AuditOutcome.Success, cancellationToken, new { step, status = EnumMap.ToApi(next) });
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordStep(
        AiWorkflow workflow,
        AgentRole agent,
        string inputSummary,
        object output,
        IEnumerable<string> toolsUsed,
        object? validation,
        CancellationToken cancellationToken,
        string? errorCode = null,
        int attempt = 1)
    {
        var started = DateTimeOffset.UtcNow;
        await steps.AddAsync(new AiWorkflowStep
        {
            WorkflowId = workflow.Id,
            AgentRole = agent,
            InputSummary = AgentSafety.SanitizeUserText(inputSummary, 2000),
            OutputJson = JsonSerializer.Serialize(output, AgentJson.Options),
            ToolName = toolsUsed.FirstOrDefault(),
            ToolArgsSummary = string.Join(",", toolsUsed),
            ToolResultSummary = errorCode is null ? "ok" : errorCode,
            ValidationJson = validation is null ? null : JsonSerializer.Serialize(validation, AgentJson.Options),
            DurationMs = 1,
            ErrorCode = errorCode,
            Attempt = attempt,
            Timestamp = started
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Workflow {WorkflowId} agent {Agent} recorded {Error}", workflow.Id, agent, errorCode ?? "ok");
    }

    private static void MergePlan(AiWorkflow workflow, string key, object value)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(workflow.PlanJson) ? "{}" : workflow.PlanJson) as JsonObject ?? new JsonObject();
        node[key] = JsonNode.Parse(JsonSerializer.Serialize(value, AgentJson.Options));
        workflow.PlanJson = node.ToJsonString();
    }

    private async Task PersistWorkflowAsync(CancellationToken cancellationToken) =>
        await unitOfWork.SaveChangesAsync(cancellationToken);

    private async Task ChangeRequestStatusAsync(ServiceRequest request, ServiceRequestStatus to, string note, CancellationToken cancellationToken)
    {
        if (request.Status == to || !RequestStateMachine.CanTransition(request.Status, to))
        {
            return;
        }

        var from = request.Status;
        request.Status = to;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await requestHistory.AddAsync(new RequestStatusHistory
        {
            RequestId = request.Id,
            ActorId = currentUser.UserId == Guid.Empty ? request.CustomerId : currentUser.UserId,
            FromStatus = from,
            ToStatus = to,
            Note = AgentSafety.SanitizeUserText(note, 1000)
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<WorkflowRunResult> Fail(AiWorkflow workflow, ServiceRequest request, Exception ex, CancellationToken cancellationToken)
    {
        var code = ex is AgentOutputException output ? output.Code : "FAILED";
        workflow.ErrorCode = code;
        workflow.OutcomeJson = JsonSerializer.Serialize(new { error = AgentSafety.SanitizeUserText(ex.Message, 500), code }, AgentJson.Options);
        if (AiWorkflowStateMachine.CanTransition(workflow.Status, AiWorkflowStatus.Failed))
        {
            workflow.Status = AiWorkflowStatus.Failed;
            workflow.CurrentStep = "FAILED";
        }

        workflow.FinishedAt = DateTimeOffset.UtcNow;
        workflow.DurationMs = (int)(workflow.FinishedAt.Value - workflow.StartedAt).TotalMilliseconds;
        await PersistWorkflowAsync(cancellationToken);
        await ChangeRequestStatusAsync(request, ServiceRequestStatus.Failed, $"Safe failure: {code}", cancellationToken);
        await Audit("WORKFLOW_FAILED", workflow.Id, AuditOutcome.Failure, cancellationToken, new { code });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogWarning(ex, "Workflow {WorkflowId} failed safely with {Code}", workflow.Id, code);
        return Map(workflow);
    }

    private Task Audit(string action, Guid entityId, AuditOutcome outcome, CancellationToken cancellationToken, object? metadata = null) =>
        audits.AddAsync(new AuditLog
        {
            ActorId = currentUser.UserId == Guid.Empty ? null : currentUser.UserId,
            Action = action,
            Entity = "AiWorkflow",
            EntityId = entityId,
            Outcome = outcome,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, AgentJson.Options)
        }, cancellationToken);

    private AgentToolContext Context(AiWorkflow workflow, Guid requestId, AgentRole agent) => new()
    {
        Agent = agent,
        WorkflowId = workflow.Id,
        RequestId = requestId,
        ActorId = currentUser.UserId
    };

    private static WorkflowRunResult Map(AiWorkflow workflow) => new()
    {
        WorkflowId = workflow.Id,
        RequestId = workflow.RequestId,
        Status = EnumMap.ToApi(AiWorkflowStateMachine.Normalize(workflow.Status)),
        CurrentStep = workflow.CurrentStep,
        PlanJson = workflow.PlanJson,
        OutcomeJson = workflow.OutcomeJson,
        ErrorCode = workflow.ErrorCode,
        ClarificationRequired = AiWorkflowStateMachine.Normalize(workflow.Status) == AiWorkflowStatus.ClarificationRequired
    };
}
