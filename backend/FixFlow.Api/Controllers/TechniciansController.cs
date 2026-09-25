using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Technicians;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "Technician")]
[Route("api/technicians")]
public class TechniciansController(ITechnicianService technicians, IReviewService reviews) : ControllerBase
{
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile(TechnicianProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.CreateProfileAsync(request, cancellationToken));

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        Ok(await technicians.GetProfileAsync(cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(TechnicianProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.UpdateProfileAsync(request, cancellationToken));

    [HttpGet("{technicianId:guid}/photo")]
    [AllowAnonymous]
    public async Task<IActionResult> Photo(Guid technicianId, CancellationToken cancellationToken)
    {
        var photo = await technicians.GetPhotoAsync(technicianId, cancellationToken);
        if (photo is null)
        {
            return NotFound();
        }

        return File(photo.Content, photo.ContentType);
    }

    [HttpGet("{technicianId:guid}/public")]
    [AllowAnonymous]
    public async Task<IActionResult> Public(Guid technicianId, CancellationToken cancellationToken) =>
        Ok(await technicians.GetPublicAsync(technicianId, cancellationToken));

    [HttpGet("{technicianId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> Reviews(Guid technicianId, CancellationToken cancellationToken) =>
        Ok(await reviews.ListForTechnicianAsync(technicianId, cancellationToken));
}

[ApiController]
[Authorize(Policy = "Technician")]
[Route("api/technician-applications")]
public class TechnicianApplicationsController(
    ITechnicianService technicians,
    IValidator<TechnicianApplicationRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Apply(TechnicianApplicationRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await technicians.ApplyAsync(request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await technicians.ListMineAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await technicians.GetMineAsync(id, cancellationToken));

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> Documents(Guid id, IFormFile file, [FromForm] string evidenceType, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A document file is required.", code = "VALIDATION_FAILED" });
        }

        await using var stream = file.OpenReadStream();
        return Ok(await technicians.AddDocumentAsync(id, evidenceType, file.FileName, file.ContentType ?? "application/octet-stream", stream, cancellationToken));
    }
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/technician-applications")]
public class AdminTechnicianApplicationsController(ITechnicianService technicians) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await technicians.AdminListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await technicians.AdminGetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.ApproveAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/request-info")]
    public async Task<IActionResult> RequestInfo(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.RequestInfoAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reverify")]
    public async Task<IActionResult> Reverify(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.RequestReverificationAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendApplication(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.SuspendApplicationAsync(id, request, cancellationToken));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/technicians")]
public class AdminTechniciansController(ITechnicianService technicians) : ControllerBase
{
    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.SuspendTechnicianAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, ApplicationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await technicians.ReactivateTechnicianAsync(id, request, cancellationToken));
}
