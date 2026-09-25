using FixFlow.Application.DTOs.Agents;
using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/workflows")]
public class AiWorkflowsController(IAiWorkflowService workflows) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Create(CreateWorkflowRequest request, CancellationToken cancellationToken) =>
        Ok(await workflows.CreateAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await workflows.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/steps")]
    public async Task<IActionResult> Steps(Guid id, CancellationToken cancellationToken) =>
        Ok(await workflows.StepsAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Approve(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await workflows.ApproveAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Reject(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await workflows.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/revise")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Revise(Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await workflows.ReviseAsync(id, request, cancellationToken));
}
