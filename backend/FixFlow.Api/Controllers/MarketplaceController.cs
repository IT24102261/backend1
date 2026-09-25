using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController(IMarketplaceService marketplace, IValidator<QuoteWriteRequest> quoteValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await marketplace.ListInvitationsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.GetInvitationAsync(id, cancellationToken));

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.AcceptInvitationAsync(id, cancellationToken));

    [HttpPost("{id:guid}/decline")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.DeclineInvitationAsync(id, cancellationToken));

    [HttpPost("{id:guid}/quote")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> Quote(Guid id, QuoteWriteRequest request, CancellationToken cancellationToken)
    {
        await quoteValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await marketplace.CreateQuoteAsync(id, request, cancellationToken));
    }
}

[ApiController]
[Authorize]
[Route("api/quotes")]
public class QuotesController(IMarketplaceService marketplace) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.GetQuoteAsync(id, cancellationToken));

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.WithdrawQuoteAsync(id, cancellationToken));

    [HttpPost("{id:guid}/select")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Select(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.SelectQuoteAsync(id, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController(IMarketplaceService marketplace, IReviewService reviews, IValidator<FixFlow.Application.DTOs.Reviews.ReviewWriteRequest> reviewValidator) : ControllerBase
{
    [HttpPost("confirm")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Confirm(ConfirmBookingRequest request, CancellationToken cancellationToken) =>
        Ok(await marketplace.ConfirmBookingAsync(request, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedQuery query, CancellationToken cancellationToken) =>
        Ok(await marketplace.ListBookingsAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.GetBookingAsync(id, cancellationToken));

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.BookingHistoryAsync(id, cancellationToken));

    [HttpGet("{id:guid}/scope-changes")]
    public async Task<IActionResult> ScopeChanges(Guid id, CancellationToken cancellationToken) =>
        Ok(await marketplace.ListScopeChangesAsync(id, cancellationToken));

    [HttpPost("{id:guid}/scope-changes")]
    [Authorize(Policy = "Technician")]
    public async Task<IActionResult> ProposeScope(Guid id, ScopeChangeWriteRequest request, CancellationToken cancellationToken) =>
        Ok(await marketplace.ProposeScopeChangeAsync(id, request, cancellationToken));

    [HttpPost("scope-changes/{id:guid}/decision")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> DecideScope(Guid id, ScopeChangeDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await marketplace.DecideScopeChangeAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, BookingStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await marketplace.UpdateBookingStatusAsync(id, request, cancellationToken));

    [HttpPost("{bookingId:guid}/reviews")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Review(Guid bookingId, FixFlow.Application.DTOs.Reviews.ReviewWriteRequest request, CancellationToken cancellationToken)
    {
        await reviewValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await reviews.CreateAsync(bookingId, request, cancellationToken));
    }
}
