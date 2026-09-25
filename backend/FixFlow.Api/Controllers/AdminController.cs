using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Admin;
using FixFlow.Application.DTOs.Complaints;
using FixFlow.Application.DTOs.Reviews;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/complaints")]
public class ComplaintsController(IComplaintService complaints, IValidator<ComplaintWriteRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(ComplaintWriteRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await complaints.CreateAsync(request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await complaints.ListAsync(query, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/reviews")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Ok(await reviews.ListMineAsync(cancellationToken));
}

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController(
    IComplaintService complaints,
    IReviewService reviews,
    IAdminUserService adminUsers,
    IReportingService reporting) : ControllerBase
{
    [HttpGet("complaints")]
    public async Task<IActionResult> Complaints([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await complaints.AdminListAsync(query, cancellationToken));

    [HttpPatch("complaints/{id:guid}/status")]
    public async Task<IActionResult> ComplaintStatus(Guid id, ComplaintStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await complaints.UpdateStatusAsync(id, request, cancellationToken));

    [HttpPost("complaints/{id:guid}/reply")]
    public async Task<IActionResult> ComplaintReply(Guid id, AdminReplyRequest request, IValidator<AdminReplyRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await complaints.ReplyAsync(id, request, cancellationToken));
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reviews.AdminListAsync(query, cancellationToken));

    [HttpPatch("reviews/{id:guid}/moderate")]
    public async Task<IActionResult> Moderate(Guid id, ModerateReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await reviews.ModerateAsync(id, request, cancellationToken));

    [HttpPatch("reviews/{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, AdminReviewUpdateRequest request, IValidator<AdminReviewUpdateRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await reviews.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("reviews/{id:guid}/reply")]
    public async Task<IActionResult> ReviewReply(Guid id, AdminReplyRequest request, IValidator<AdminReplyRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await reviews.ReplyAsync(id, request, cancellationToken));
    }

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        await reviews.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await adminUsers.ListAsync(query, cancellationToken));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(AdminCreateUserRequest request, IValidator<AdminCreateUserRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await adminUsers.CreateAsync(request, cancellationToken));
    }

    [HttpPatch("users/{id:guid}/status")]
    public async Task<IActionResult> UserStatus(Guid id, AdminUserStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await adminUsers.SetActiveAsync(id, request, cancellationToken));

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        Ok(await reporting.DashboardAsync(cancellationToken));

    [HttpGet("reports/requests")]
    public async Task<IActionResult> RequestReports([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reporting.RequestsAsync(query, cancellationToken));

    [HttpGet("reports/bookings")]
    public async Task<IActionResult> BookingReports([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reporting.BookingsAsync(query, cancellationToken));

    [HttpGet("reports/technicians")]
    public async Task<IActionResult> TechnicianReports([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reporting.TechniciansAsync(query, cancellationToken));

    [HttpGet("reports/agents")]
    public async Task<IActionResult> AgentReports([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reporting.AgentsAsync(query, cancellationToken));

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await reporting.AuditAsync(query, cancellationToken));
}
