using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.DTOs.Quotes;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FixFlow.Application.Services;

public class MarketplaceService(
    IRepository<RequestInvitation> invitations,
    IRepository<ServiceRequest> requests,
    IRepository<Quotation> quotations,
    IRepository<Booking> bookings,
    IRepository<BookingStatusHistory> bookingHistory,
    IRepository<ScopeChangeRequest> scopeChanges,
    IRepository<TechnicianProfile> profiles,
    IRepository<TechnicianCategoryApplication> applications,
    IAgentOrchestrator orchestrator,
    IMapService maps,
    INotificationService notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<MarketplaceService> logger) : IMarketplaceService
{
    public async Task<IReadOnlyList<InvitationDto>> ListInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var profile = await RequireProfile(cancellationToken);
        var items = await invitations.Query()
            .Include(x => x.Request).ThenInclude(x => x.Category)
            .Where(x =>
                x.TechnicianId == profile.Id
                && x.Request.Status != ServiceRequestStatus.Cancelled
                && (x.Request.Description == null || !x.Request.Description.StartsWith("[Demo]")))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
        var booked = await BookedRequestIdsAsync(items.Select(x => x.RequestId), cancellationToken);
        return items.Select(item => MapInvitation(item, !booked.Contains(item.RequestId))).ToList();
    }

    public async Task<InvitationDto> GetInvitationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadInvitation(id, cancellationToken);
        await EnsureInvitationAccess(invitation, cancellationToken);
        var booked = await HasActiveBookingAsync(invitation.RequestId, cancellationToken);
        return MapInvitation(invitation, !booked);
    }

    public async Task<InvitationDto> AcceptInvitationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadInvitation(id, cancellationToken);
        var profile = await EnsureInvitationAccess(invitation, cancellationToken);
        await EnsureRequestOpenForQuotes(invitation.Request, cancellationToken);
        await EnsureCategoryApproved(profile.Id, invitation.Request.CategoryId, cancellationToken);
        invitation.Status = InvitationStatus.Accepted;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapInvitation(invitation, true);
    }

    public async Task<InvitationDto> DeclineInvitationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await LoadInvitation(id, cancellationToken);
        await EnsureInvitationAccess(invitation, cancellationToken);
        invitation.Status = InvitationStatus.Declined;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapInvitation(invitation, false);
    }

    public async Task<QuoteDto> CreateQuoteAsync(Guid invitationId, QuoteWriteRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTotals(request);
        var invitation = await LoadInvitation(invitationId, cancellationToken);
        var profile = await EnsureInvitationAccess(invitation, cancellationToken);
        await EnsureRequestOpenForQuotes(invitation.Request, cancellationToken);
        await EnsureCategoryApproved(profile.Id, invitation.Request.CategoryId, cancellationToken);
        if (invitation.Status == InvitationStatus.Declined || invitation.Status == InvitationStatus.Expired)
        {
            throw new ConflictException("Cannot quote a declined invitation.");
        }

        invitation.Status = InvitationStatus.Accepted;
        var previous = await quotations.Query()
            .Where(x => x.InvitationId == invitationId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (previous is { Status: QuotationStatus.Sent })
        {
            previous.Status = QuotationStatus.Withdrawn;
        }

        var quote = new Quotation
        {
            RequestId = invitation.RequestId,
            TechnicianId = profile.Id,
            InvitationId = invitation.Id,
            QuoteGroupId = previous?.QuoteGroupId ?? Guid.NewGuid(),
            LabourAmount = request.LabourAmount,
            MaterialsAmount = request.MaterialsAmount,
            TravelAmount = request.TravelAmount,
            TotalAmount = request.TotalAmount,
            Currency = request.Currency.ToUpperInvariant(),
            DurationMinutes = request.DurationMinutes is > 0 ? request.DurationMinutes.Value : 60,
            ArrivalStart = request.ArrivalStart?.ToUniversalTime(),
            ExpiresAt = request.ExpiresAt is { } expires && expires > DateTimeOffset.UtcNow
                ? expires.ToUniversalTime()
                : DateTimeOffset.UtcNow.AddDays(7),
            Assumptions = request.Assumptions,
            IncludedMaterials = request.IncludedMaterials,
            ExcludedMaterials = request.ExcludedMaterials,
            Status = QuotationStatus.Sent,
            Version = (previous?.Version ?? 0) + 1
        };
        await quotations.AddAsync(quote, cancellationToken);

        if (invitation.Request.Status == ServiceRequestStatus.Matching)
        {
            invitation.Request.Status = ServiceRequestStatus.CollectingQuotes;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        try
        {
            await orchestrator.CollectAndRecommendAsync(invitation.RequestId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Quote {QuoteId} was saved but recommendation did not complete for request {RequestId}.", quote.Id, invitation.RequestId);
        }

        return MapQuote(quote);
    }

    public async Task<IReadOnlyList<QuoteDto>> ListQuotesAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await requests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("Request not found.");
        var owner = currentUser.IsAdmin || request.CustomerId == currentUser.UserId;
        TechnicianProfile? viewerProfile = null;
        if (!owner)
        {
            viewerProfile = await profiles.FirstAsync(x => x.UserId == currentUser.UserId, cancellationToken);
            if (viewerProfile is null)
            {
                throw new ForbiddenException();
            }
        }

        var now = DateTimeOffset.UtcNow;
        var items = await quotations.Query()
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .Where(x => x.RequestId == requestId && x.Status != QuotationStatus.Withdrawn)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        if (!currentUser.IsAdmin)
        {
            items = items.Where(x =>
                (x.Status == QuotationStatus.Sent || (owner && x.Status == QuotationStatus.Accepted))
                && !x.Technician.IsSuspended
                && (owner || x.ExpiresAt > now)).ToList();
        }

        if (viewerProfile is not null)
        {
            items = items.Where(x => x.TechnicianId == viewerProfile.Id).ToList();
        }

        var technicianIds = items.Select(x => x.TechnicianId).Distinct().ToList();
        var completed = await bookings.Query()
            .Where(x => technicianIds.Contains(x.TechnicianId)
                && (x.Status == BookingStatus.Closed || x.Status == BookingStatus.CustomerConfirmed))
            .ToListAsync(cancellationToken);
        var completedByTechnician = completed.GroupBy(x => x.TechnicianId).ToDictionary(x => x.Key, x => x.Count());

        var result = new List<QuoteDto>();
        foreach (var quote in items)
        {
            var technician = quote.Technician ?? await profiles.GetByIdAsync(quote.TechnicianId, cancellationToken);
            var dto = MapQuote(quote);
            dto.TechnicianDisplayName = technician?.User?.DisplayName;
            dto.AverageRating = technician?.AverageRating;
            dto.ReviewCount = technician?.ReviewCount;
            dto.CompletedJobs = completedByTechnician.GetValueOrDefault(quote.TechnicianId);
            dto.ProfilePhotoUrl = TechnicianPhotoUrl.For(quote.TechnicianId, technician?.ProfilePhotoStorageKey);
            await AttachApproximateDistance(dto, request, technician, cancellationToken);
            result.Add(dto);
        }

        if (owner)
        {
            ApplyRecommendationExplanations(result, request.PreferredStart);
        }

        return result;
    }

    public async Task<QuoteDto> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quote = await LoadQuote(id, cancellationToken);
        var request = await requests.GetByIdAsync(quote.RequestId, cancellationToken);
        var owner = currentUser.IsAdmin || request?.CustomerId == currentUser.UserId;
        if (!owner)
        {
            var viewerProfile = await profiles.FirstAsync(x => x.UserId == currentUser.UserId, cancellationToken);
            if (viewerProfile is null || quote.TechnicianId != viewerProfile.Id)
            {
                throw new ForbiddenException("Technicians can only view their own quotations.");
            }
        }

        var dto = MapQuote(quote);
        var technician = await profiles.GetByIdAsync(quote.TechnicianId, cancellationToken);
        dto.TechnicianDisplayName = technician?.User?.DisplayName;
        dto.AverageRating = technician?.AverageRating;
        dto.ReviewCount = technician?.ReviewCount;
        dto.ProfilePhotoUrl = TechnicianPhotoUrl.For(quote.TechnicianId, technician?.ProfilePhotoStorageKey);
        if (request is not null)
        {
            await AttachApproximateDistance(dto, request, technician, cancellationToken);
        }

        return dto;
    }

    public async Task<QuoteDto> WithdrawQuoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quote = await LoadQuote(id, cancellationToken);
        var profile = await RequireProfile(cancellationToken);
        if (quote.TechnicianId != profile.Id && !currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        if (quote.Status != QuotationStatus.Sent)
        {
            throw new ConflictException("Only submitted quotes can be withdrawn.");
        }

        quote.Status = QuotationStatus.Withdrawn;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapQuote(quote);
    }

    public async Task<BookingDto> SelectQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await quotations.Query()
            .Include(x => x.Request).ThenInclude(x => x.Customer)
            .Include(x => x.Request).ThenInclude(x => x.Category)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");

        if (!currentUser.IsAdmin && quote.Request.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the owning customer can select a quote.");
        }

        if (quote.Status != QuotationStatus.Sent)
        {
            throw new ConflictException("Only a submitted quote can be booked.");
        }

        if (quote.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ConflictException("This quote has expired.");
        }

        await EnsureCategoryApproved(quote.TechnicianId, quote.Request.CategoryId, cancellationToken);
        if (quote.Technician.IsSuspended)
        {
            throw new ConflictException("This technician is suspended.");
        }

        var hasActive = await bookings.Query().AnyAsync(
            x => x.RequestId == quote.RequestId && BookingStateMachine.Active.Contains(x.Status),
            cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("This request already has an active booking.");
        }

        await EnsureNoScheduleConflict(quote, cancellationToken);

        await orchestrator.RecordCustomerDecisionAsync(
            quote.RequestId,
            ApprovalDecision.Approved,
            $"Selected quotation {quote.Id}",
            cancellationToken);

        var validation = await orchestrator.ValidateSelectedQuoteAsync(
            quote.RequestId,
            quote.Id,
            currentUser.UserId == Guid.Empty ? quote.Request.CustomerId : currentUser.UserId,
            cancellationToken);
        if (validation.Validation?.BookingAllowed != true)
        {
            throw new ConflictException(string.Join(" ", validation.Validation?.Errors ?? ["Booking validation failed."]));
        }

        var created = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            quote.Status = QuotationStatus.Accepted;
            quote.Request.Status = ServiceRequestStatus.AwaitingCustomerApproval;
            var booking = new Booking
            {
                RequestId = quote.RequestId,
                QuotationId = quote.Id,
                CustomerId = quote.Request.CustomerId,
                TechnicianId = quote.TechnicianId,
                Status = BookingStatus.PendingValidation,
                Request = quote.Request,
                Quotation = quote,
                Customer = quote.Request.Customer,
                Technician = quote.Technician
            };
            await bookings.AddAsync(booking, cancellationToken);
            await bookingHistory.AddAsync(new BookingStatusHistory
            {
                BookingId = booking.Id,
                ActorId = currentUser.UserId,
                FromStatus = BookingStatus.PendingValidation,
                ToStatus = BookingStatus.PendingValidation,
                Note = "Quote selected after Agent 4 validation"
            }, cancellationToken);
            await orchestrator.CompleteAsync(quote.RequestId, new { bookingId = booking.Id, quotationId = quote.Id }, cancellationToken);
            logger.LogInformation("Quote {QuoteId} selected for request {RequestId}", quote.Id, quote.RequestId);
            return MapBooking(booking, includeAddress: false);
        }, cancellationToken);

        var bookingLabel = created.Id.ToString("N")[..8];
        var customerName = quote.Request.Customer?.DisplayName ?? "A customer";
        var service = quote.Request.Category?.Name;
        await notifications.NotifyAsync(
            quote.Technician.UserId,
            "New job booking",
            service is null
                ? $"{customerName} booked you from your quotation. Booking {bookingLabel}. Open Jobs to view the request."
                : $"{customerName} booked you for {service}. Booking {bookingLabel}. Open Jobs to view the request.",
            cancellationToken);
        await NotifyOtherTechniciansJobTakenAsync(quote.RequestId, quote.TechnicianId, service, cancellationToken);
        return created;
    }

    public async Task<BookingDto> ConfirmBookingAsync(ConfirmBookingRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(request.BookingId, cancellationToken);
        if (!currentUser.IsAdmin && booking.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the booking customer can confirm.");
        }

        if (booking.Status != BookingStatus.PendingValidation)
        {
            throw new ConflictException("Only a selected quotation waiting for confirmation can be confirmed.");
        }

        var failure = await ConfirmValidationFailureAsync(booking, cancellationToken);
        if (failure is not null)
        {
            await ChangeBookingStatusAsync(booking, BookingStatus.Cancelled, failure, cancellationToken);
            booking.Quotation.Status = QuotationStatus.Sent;
            booking.Request.Status = ServiceRequestStatus.CollectingQuotes;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ConflictException(failure);
        }

        var confirmed = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await ChangeBookingStatusAsync(booking, BookingStatus.Confirmed, "Customer confirmed booking after Agent 4 validation", cancellationToken);
            booking.ConfirmedAt = DateTimeOffset.UtcNow;
            booking.ApprovedAt = DateTimeOffset.UtcNow;
            booking.AddressReleaseAt = DateTimeOffset.UtcNow;
            booking.Request.Status = ServiceRequestStatus.Booked;
            logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
            return MapBooking(booking, includeAddress: true);
        }, cancellationToken);

        var bookingLabel = confirmed.Id.ToString("N")[..8];
        var customerName = booking.Customer?.DisplayName ?? "A customer";
        await notifications.NotifyAsync(
            booking.Technician.UserId,
            "Booking confirmed",
            $"{customerName} confirmed booking {bookingLabel}. The job address is now available in Jobs.",
            cancellationToken);
        return confirmed;
    }

    public async Task<PagedResult<BookingDto>> ListBookingsAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var source = bookings.Query()
            .Include(x => x.Request).ThenInclude(x => x.Category)
            .Include(x => x.Customer)
            .Include(x => x.Quotation)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .AsQueryable();
        if (currentUser.IsCustomer)
        {
            source = source.Where(x => x.CustomerId == currentUser.UserId);
        }
        else if (currentUser.IsTechnician)
        {
            var profile = await RequireProfile(cancellationToken);
            source = source.Where(x => x.TechnicianId == profile.Id);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = EnumMap.Parse<BookingStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.ConfirmedAt).Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        var viewer = currentUser.IsTechnician ? await RequireProfile(cancellationToken) : null;
        return new PagedResult<BookingDto>
        {
            Items = items.Select(x => MapBooking(x, CanSeeBookingAddress(x, viewer))).ToList(),
            Page = query.Page,
            PageSize = query.Take,
            TotalCount = total
        };
    }

    public async Task<BookingDto> GetBookingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(id, cancellationToken);
        var viewer = currentUser.IsTechnician ? await RequireProfile(cancellationToken) : null;
        EnsureCanViewBooking(booking, viewer);
        return MapBooking(booking, CanSeeBookingAddress(booking, viewer));
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(Guid id, BookingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(id, cancellationToken);
        var next = EnumMap.Parse<BookingStatus>(request.Status);
        if (!BookingStateMachine.CanTransition(booking.Status, next))
        {
            throw new ConflictException($"Cannot move booking from {EnumMap.ToApi(booking.Status)} to {EnumMap.ToApi(next)}.");
        }

        if (next == BookingStatus.CustomerConfirmed)
        {
            if (booking.CustomerId != currentUser.UserId && !currentUser.IsAdmin)
            {
                throw new ForbiddenException("Only the booking customer can confirm completion.");
            }
        }
        else if (BookingStateMachine.TechnicianOperational.Contains(next))
        {
            var profile = await RequireProfile(cancellationToken);
            if (booking.TechnicianId != profile.Id && !currentUser.IsAdmin)
            {
                throw new ForbiddenException("Only the assigned technician can change operational status.");
            }
        }
        else if (!currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        await ChangeBookingStatusAsync(booking, next, request.Note, cancellationToken);
        if (next == BookingStatus.CustomerConfirmed)
        {
            booking.Request.Status = ServiceRequestStatus.Completed;
        }

        if (next == BookingStatus.Cancelled)
        {
            booking.Request.Status = ServiceRequestStatus.Cancelled;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var viewer = currentUser.IsTechnician ? await RequireProfile(cancellationToken) : null;
        return MapBooking(booking, CanSeeBookingAddress(booking, viewer));
    }

    public async Task<IReadOnlyList<BookingHistoryDto>> BookingHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(id, cancellationToken);
        var viewer = currentUser.IsTechnician ? await RequireProfile(cancellationToken) : null;
        EnsureCanViewBooking(booking, viewer);
        var items = await bookingHistory.Query()
            .Where(x => x.BookingId == id)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return items.Select(x => new BookingHistoryDto
        {
            Id = x.Id,
            FromStatus = EnumMap.ToApi(x.FromStatus),
            ToStatus = EnumMap.ToApi(x.ToStatus),
            Note = x.Note,
            Timestamp = x.Timestamp
        }).ToList();
    }

    public async Task<ScopeChangeDto> ProposeScopeChangeAsync(Guid bookingId, ScopeChangeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(bookingId, cancellationToken);
        var profile = await RequireProfile(cancellationToken);
        if (booking.TechnicianId != profile.Id && !currentUser.IsAdmin)
        {
            throw new ForbiddenException("Only the assigned technician can propose extra work.");
        }

        if (booking.Status is BookingStatus.Closed or BookingStatus.Cancelled)
        {
            throw new ConflictException("Scope cannot change on a closed or cancelled booking.");
        }

        var change = new ScopeChangeRequest
        {
            BookingId = bookingId,
            Description = request.Description.Trim(),
            ProposedCost = request.ProposedCost,
            CustomerDecision = CustomerDecision.Pending
        };
        await scopeChanges.AddAsync(change, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapScope(change);
    }

    public async Task<IReadOnlyList<ScopeChangeDto>> ListScopeChangesAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBooking(bookingId, cancellationToken);
        var viewer = currentUser.IsTechnician ? await RequireProfile(cancellationToken) : null;
        EnsureCanViewBooking(booking, viewer);
        var items = await scopeChanges.Query().Where(x => x.BookingId == bookingId).OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return items.Select(MapScope).ToList();
    }

    public async Task<ScopeChangeDto> DecideScopeChangeAsync(Guid id, ScopeChangeDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var change = await scopeChanges.Query().Include(x => x.Booking).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Scope change not found.");
        if (!currentUser.IsAdmin && change.Booking.CustomerId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the booking customer can approve extra work.");
        }

        if (change.CustomerDecision != CustomerDecision.Pending)
        {
            throw new ConflictException("This scope change already has a decision.");
        }

        change.CustomerDecision = EnumMap.Parse<CustomerDecision>(request.Decision);
        if (change.CustomerDecision == CustomerDecision.Pending)
        {
            throw new ConflictException("A decision of accepted or rejected is required.");
        }

        change.DecisionAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapScope(change);
    }

    private Task ChangeBookingStatusAsync(Booking booking, BookingStatus to, string? note, CancellationToken cancellationToken)
    {
        if (!BookingStateMachine.CanTransition(booking.Status, to))
        {
            throw new ConflictException($"Cannot move booking from {EnumMap.ToApi(booking.Status)} to {EnumMap.ToApi(to)}.");
        }

        var from = booking.Status;
        booking.Status = to;
        return bookingHistory.AddAsync(new BookingStatusHistory
        {
            BookingId = booking.Id,
            ActorId = currentUser.UserId,
            FromStatus = from,
            ToStatus = to,
            Note = note
        }, cancellationToken);
    }

    private async Task<string?> ConfirmValidationFailureAsync(Booking booking, CancellationToken cancellationToken)
    {
        var quote = booking.Quotation;
        var technician = booking.Technician ?? await profiles.Query()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == booking.TechnicianId, cancellationToken);
        if (technician is null || technician.IsSuspended || technician.User is { IsActive: false })
        {
            return "The selected technician is no longer available.";
        }

        try
        {
            await EnsureCategoryApproved(booking.TechnicianId, booking.Request.CategoryId, cancellationToken);
        }
        catch (Exception ex) when (ex is ForbiddenException or ConflictException)
        {
            return "The selected technician is no longer verified for this category.";
        }

        if (quote.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return "The selected quotation is no longer valid.";
        }

        if (quote.ArrivalStart is not null)
        {
            var start = quote.ArrivalStart.Value;
            var end = start.AddMinutes(quote.DurationMinutes);
            var conflict = await bookings.Query()
                .Include(x => x.Quotation)
                .AnyAsync(x =>
                    x.Id != booking.Id
                    && x.TechnicianId == quote.TechnicianId
                    && BookingStateMachine.Active.Contains(x.Status)
                    && x.Quotation.ArrivalStart != null
                    && x.Quotation.ArrivalStart < end
                    && x.Quotation.ArrivalStart.Value.AddMinutes(x.Quotation.DurationMinutes) > start,
                    cancellationToken);
            if (conflict)
            {
                return "The selected technician is no longer available.";
            }
        }

        return null;
    }

    private async Task EnsureNoScheduleConflict(Quotation quote, CancellationToken cancellationToken)
    {
        if (quote.ArrivalStart is null)
        {
            return;
        }

        var start = quote.ArrivalStart.Value;
        var end = start.AddMinutes(quote.DurationMinutes);
        var conflict = await bookings.Query()
            .Include(x => x.Quotation)
            .AnyAsync(x =>
                x.TechnicianId == quote.TechnicianId
                && BookingStateMachine.Active.Contains(x.Status)
                && x.Quotation.ArrivalStart != null
                && x.Quotation.ArrivalStart < end
                && x.Quotation.ArrivalStart.Value.AddMinutes(x.Quotation.DurationMinutes) > start,
                cancellationToken);

        if (conflict)
        {
            throw new ConflictException("This technician already has a booking in that time slot.");
        }
    }

    private async Task EnsureCategoryApproved(Guid technicianId, Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            throw new ConflictException("Request category is required to book a technician.");
        }

        var approved = await applications.Query().AnyAsync(
            x => x.TechnicianId == technicianId && x.CategoryId == categoryId && x.Status == ApplicationStatus.Approved,
            cancellationToken);
        if (!approved)
        {
            throw new ForbiddenException("Technician is not approved for this category.");
        }
    }

    private static void ValidateTotals(QuoteWriteRequest request)
    {
        if (request.LabourAmount + request.MaterialsAmount + request.TravelAmount != request.TotalAmount)
        {
            throw new AppException("Total must equal labour + materials + travel.", 400, "INVALID_QUOTE_TOTAL");
        }
    }

    private async Task EnsureRequestOpenForQuotes(ServiceRequest request, CancellationToken cancellationToken)
    {
        if (!ServiceRequestRules.AllowsQuotations(request.Status) || await HasActiveBookingAsync(request.Id, cancellationToken))
        {
            throw new ConflictException("This job has already been accepted by another technician. Quotations are no longer accepted.");
        }
    }

    private async Task<bool> HasActiveBookingAsync(Guid requestId, CancellationToken cancellationToken) =>
        await bookings.Query().AnyAsync(
            x => x.RequestId == requestId && BookingStateMachine.Active.Contains(x.Status),
            cancellationToken);

    private async Task<HashSet<Guid>> BookedRequestIdsAsync(IEnumerable<Guid> requestIds, CancellationToken cancellationToken)
    {
        var ids = requestIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var booked = await bookings.Query()
            .Where(x => ids.Contains(x.RequestId) && BookingStateMachine.Active.Contains(x.Status))
            .Select(x => x.RequestId)
            .ToListAsync(cancellationToken);
        return booked.ToHashSet();
    }

    private async Task NotifyOtherTechniciansJobTakenAsync(
        Guid requestId,
        Guid bookedTechnicianId,
        string? service,
        CancellationToken cancellationToken)
    {
        var invited = await invitations.Query()
            .Where(x => x.RequestId == requestId && x.TechnicianId != bookedTechnicianId)
            .Select(x => x.Technician.UserId)
            .ToListAsync(cancellationToken);
        var quoted = await quotations.Query()
            .Where(x => x.RequestId == requestId && x.TechnicianId != bookedTechnicianId)
            .Select(x => x.Technician.UserId)
            .ToListAsync(cancellationToken);

        var others = invited.Concat(quoted).Distinct().ToList();
        var message = service is null
            ? "This job has already been accepted by another technician."
            : $"This job has already been accepted by another technician. The {service} request is no longer open.";

        foreach (var userId in others)
        {
            await notifications.NotifyAsync(userId, "This job has already been accepted by another technician.", message, cancellationToken);
        }
    }

    private async Task<RequestInvitation> LoadInvitation(Guid id, CancellationToken cancellationToken) =>
        await invitations.Query()
            .Include(x => x.Request).ThenInclude(x => x.Category)
            .Include(x => x.Technician)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Invitation not found.");

    private async Task<Quotation> LoadQuote(Guid id, CancellationToken cancellationToken) =>
        await quotations.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Quote not found.");

    private async Task<Booking> LoadBooking(Guid id, CancellationToken cancellationToken) =>
        await bookings.Query()
            .Include(x => x.Request).ThenInclude(x => x.Category)
            .Include(x => x.Customer)
            .Include(x => x.Quotation)
            .Include(x => x.Technician).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Booking not found.");

    private async Task<TechnicianProfile> RequireProfile(CancellationToken cancellationToken) =>
        await profiles.FirstAsync(x => x.UserId == currentUser.UserId, cancellationToken)
        ?? throw new ForbiddenException("Technician profile is required.");

    private async Task<TechnicianProfile> EnsureInvitationAccess(RequestInvitation invitation, CancellationToken cancellationToken)
    {
        var profile = await RequireProfile(cancellationToken);
        if (invitation.TechnicianId != profile.Id && !currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        return profile;
    }

    private void EnsureCanViewBooking(Booking booking, TechnicianProfile? profile)
    {
        if (currentUser.IsAdmin || booking.CustomerId == currentUser.UserId)
        {
            return;
        }

        if (profile is not null && booking.TechnicianId == profile.Id)
        {
            return;
        }

        throw new ForbiddenException();
    }

    private bool CanSeeBookingAddress(Booking booking, TechnicianProfile? profile)
    {
        if (currentUser.IsAdmin || booking.CustomerId == currentUser.UserId)
        {
            return true;
        }

        return profile is not null
            && booking.TechnicianId == profile.Id
            && booking.ConfirmedAt is not null
            && booking.AddressReleaseAt is not null
            && booking.Status is not BookingStatus.PendingValidation and not BookingStatus.Cancelled;
    }

    private async Task AttachApproximateDistance(
        QuoteDto dto,
        ServiceRequest request,
        TechnicianProfile? technician,
        CancellationToken cancellationToken)
    {
        var distance = await maps.CalculateApproximateDistanceAsync(
            new MapPoint
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Address = null,
                ServiceArea = request.ServiceArea
            },
            new MapPoint
            {
                Latitude = technician?.LatitudeApprox,
                Longitude = technician?.LongitudeApprox,
                Address = null,
                ServiceArea = technician?.ServiceArea
            },
            cancellationToken);

        dto.DistanceUnavailable = distance.DistanceUnavailable;
        dto.ApproximateDistanceKm = distance.DistanceUnavailable ? null : distance.DistanceKm;
        dto.DistanceBand = distance.DistanceUnavailable ? null : distance.Band;
    }

    private static InvitationDto MapInvitation(RequestInvitation invitation, bool requestStillOpen)
    {
        var requestStatus = invitation.Request.Status;
        return new InvitationDto
        {
            Id = invitation.Id,
            RequestId = invitation.RequestId,
            TechnicianId = invitation.TechnicianId,
            Status = EnumMap.ToApi(invitation.Status),
            SentAt = invitation.SentAt,
            ServiceArea = invitation.Request.ServiceArea,
            CategoryName = invitation.Request.Category?.Name,
            Description = invitation.Request.Description,
            RequestStatus = EnumMap.ToApi(requestStatus),
            CanQuote = requestStillOpen
                && invitation.Status is not InvitationStatus.Declined and not InvitationStatus.Expired
                && ServiceRequestRules.AllowsQuotations(requestStatus)
        };
    }

    private static void ApplyRecommendationExplanations(List<QuoteDto> quotes, DateTimeOffset? preferredStart)
    {
        if (quotes.Count == 0)
        {
            return;
        }

        var minTotal = quotes.Min(x => x.TotalAmount);
        var earliest = quotes.Where(x => x.ArrivalStart is not null).MinBy(x => x.ArrivalStart);
        var nearest = quotes.Where(x => x.ApproximateDistanceKm is not null).MinBy(x => x.ApproximateDistanceKm);
        var mostReviews = quotes.MaxBy(x => x.ReviewCount ?? 0);
        var mostJobs = quotes.MaxBy(x => x.CompletedJobs ?? 0);
        var closestToPreferred = preferredStart is null
            ? null
            : quotes.Where(x => x.ArrivalStart is not null)
                .MinBy(x => Math.Abs((x.ArrivalStart!.Value - preferredStart.Value).TotalMinutes));
        foreach (var quote in quotes)
        {
            var strengths = new List<string>();
            var tradeoffs = new List<string>();
            if (quote.TotalAmount == minTotal)
            {
                strengths.Add("Lowest submitted total.");
            }
            else
            {
                tradeoffs.Add($"Estimated total is {quote.TotalAmount - minTotal:0.##} {quote.Currency} higher than the lowest quote.");
            }

            if (earliest is not null && quote.Id == earliest.Id)
            {
                strengths.Add("Earliest proposed arrival.");
            }
            else if (earliest?.ArrivalStart is not null && quote.ArrivalStart is not null)
            {
                tradeoffs.Add("Proposed arrival is later than another valid quotation.");
            }

            if (closestToPreferred is not null && quote.Id == closestToPreferred.Id)
            {
                strengths.Add("Matches your preferred service time.");
            }

            if (nearest is not null && quote.Id == nearest.Id && quote.ApproximateDistanceKm is not null)
            {
                strengths.Add($"Closer to the service location (~{quote.ApproximateDistanceKm} km).");
            }
            else if (nearest?.ApproximateDistanceKm is not null && quote.ApproximateDistanceKm is not null
                && quote.ApproximateDistanceKm > nearest.ApproximateDistanceKm)
            {
                tradeoffs.Add("Farther from the service location than another valid quotation.");
            }

            if (mostReviews is not null && quote.Id == mostReviews.Id && (quote.ReviewCount ?? 0) > 0)
            {
                strengths.Add("Stronger verified review history among these options.");
            }

            if (mostJobs is not null && quote.Id == mostJobs.Id && (quote.CompletedJobs ?? 0) > 0)
            {
                strengths.Add("More completed jobs among these options.");
            }

            quote.Strengths = strengths;
            quote.Tradeoffs = tradeoffs;
            quote.RecommendationSummary = strengths.Count > 0
                ? string.Join(" ", strengths) + " You choose the technician. AI does not book automatically."
                : (tradeoffs.Count > 0 ? string.Join(" ", tradeoffs) + " Compare these factors and confirm the booking yourself." : "Eligible quotation. AI does not book automatically.");
        }
    }

    private static QuoteDto MapQuote(Quotation quote) => new()
    {
        Id = quote.Id,
        RequestId = quote.RequestId,
        TechnicianId = quote.TechnicianId,
        QuoteGroupId = quote.QuoteGroupId,
        LabourAmount = quote.LabourAmount,
        MaterialsAmount = quote.MaterialsAmount,
        TravelAmount = quote.TravelAmount,
        TotalAmount = quote.TotalAmount,
        Currency = quote.Currency,
        DurationMinutes = quote.DurationMinutes,
        ArrivalStart = quote.ArrivalStart,
        ExpiresAt = quote.ExpiresAt,
        Assumptions = quote.Assumptions,
        IncludedMaterials = quote.IncludedMaterials,
        ExcludedMaterials = quote.ExcludedMaterials,
        Status = EnumMap.ToApi(quote.Status),
        Version = quote.Version
    };

    private static BookingDto MapBooking(Booking booking, bool includeAddress) => new()
    {
        Id = booking.Id,
        RequestId = booking.RequestId,
        QuotationId = booking.QuotationId,
        CustomerId = booking.CustomerId,
        TechnicianId = booking.TechnicianId,
        Status = EnumMap.ToApi(booking.Status),
        ApprovedAt = booking.ApprovedAt,
        ConfirmedAt = booking.ConfirmedAt,
        AddressReleaseAt = booking.AddressReleaseAt,
        Address = includeAddress ? booking.Request.AddressEncrypted : null,
        Latitude = includeAddress ? booking.Request.Latitude : null,
        Longitude = includeAddress ? booking.Request.Longitude : null,
        TechnicianDisplayName = booking.Technician?.User?.DisplayName,
        CustomerDisplayName = booking.Customer?.DisplayName,
        CustomerPhone = booking.Customer?.Phone,
        CategoryName = booking.Request.Category?.Name,
        RequestDescription = booking.Request.Description,
        ServiceArea = booking.Request.ServiceArea,
        PreferredStart = booking.Request.PreferredStart,
        QuoteTotalAmount = booking.Quotation?.TotalAmount,
        Currency = booking.Quotation?.Currency,
        ProfilePhotoUrl = TechnicianPhotoUrl.For(booking.TechnicianId, booking.Technician?.ProfilePhotoStorageKey),
        Version = booking.Version
    };

    private static ScopeChangeDto MapScope(ScopeChangeRequest change) => new()
    {
        Id = change.Id,
        BookingId = change.BookingId,
        ProposedCost = change.ProposedCost,
        Description = change.Description,
        CustomerDecision = EnumMap.ToApi(change.CustomerDecision),
        DecisionAt = change.DecisionAt
    };
}
