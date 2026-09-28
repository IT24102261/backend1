using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Requests;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/requests")]
public class RequestsController(
    IRequestService requests,
    IMarketplaceService marketplace,
    IValidator<RequestWriteRequest> validator,
    IValidator<ClarificationRequest> clarificationValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Create(RequestWriteRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await requests.CreateAsync(request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await requests.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Update(Guid id, RequestWriteRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await requests.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await requests.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.SubmitAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.CancelAsync(id, cancellationToken));

    [HttpPost("{id:guid}/media")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Media(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A media file is required.", code = "VALIDATION_FAILED" });
        }

        await using var stream = file.OpenReadStream();
        return Ok(await requests.AddMediaAsync(id, file.FileName, file.ContentType ?? "application/octet-stream", stream, cancellationToken));
    }

    [HttpPost("{id:guid}/clarification")]
    public async Task<IActionResult> Clarification(Guid id, ClarificationRequest request, CancellationToken cancellationToken)
    {
        await clarificationValidator.ValidateAndThrowAsync(request, cancellationToken);
        await requests.AddClarificationAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.HistoryAsync(id, cancellationToken));

    [HttpGet("{requestId:guid}/quotes")]
    public async Task<IActionResult> Quotes(Guid requestId, CancellationToken cancellationToken) =>
        Ok(await marketplace.ListQuotesAsync(requestId, cancellationToken));
}
