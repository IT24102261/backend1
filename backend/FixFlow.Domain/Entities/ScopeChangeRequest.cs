using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class ScopeChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public decimal ProposedCost { get; set; }
    public string Description { get; set; } = string.Empty;
    public CustomerDecision CustomerDecision { get; set; } = CustomerDecision.Pending;
    public DateTimeOffset? DecisionAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
