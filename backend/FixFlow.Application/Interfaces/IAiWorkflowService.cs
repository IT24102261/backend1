using FixFlow.Application.DTOs.Agents;

namespace FixFlow.Application.Interfaces;

public interface IAiWorkflowService
{
    Task<WorkflowDto> CreateAsync(CreateWorkflowRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowStepDto>> StepsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowDto> ApproveAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowDto> RejectAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowDto> ReviseAsync(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default);
}
