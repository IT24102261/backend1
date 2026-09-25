using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Quotes;

namespace FixFlow.Application.Interfaces;

public interface IMarketplaceService
{
    Task<IReadOnlyList<InvitationDto>> ListInvitationsAsync(CancellationToken cancellationToken = default);
    Task<InvitationDto> GetInvitationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvitationDto> AcceptInvitationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvitationDto> DeclineInvitationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuoteDto> CreateQuoteAsync(Guid invitationId, QuoteWriteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuoteDto>> ListQuotesAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<QuoteDto> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<QuoteDto> WithdrawQuoteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingDto> SelectQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);
    Task<BookingDto> ConfirmBookingAsync(ConfirmBookingRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<BookingDto>> ListBookingsAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<BookingDto> GetBookingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingDto> UpdateBookingStatusAsync(Guid id, BookingStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingHistoryDto>> BookingHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ScopeChangeDto> ProposeScopeChangeAsync(Guid bookingId, ScopeChangeWriteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScopeChangeDto>> ListScopeChangesAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<ScopeChangeDto> DecideScopeChangeAsync(Guid id, ScopeChangeDecisionRequest request, CancellationToken cancellationToken = default);
}
