using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingValidation;
    public DateTimeOffset? AddressReleaseAt { get; set; }
    public int Version { get; set; } = 1;

    public ServiceRequest Request { get; set; } = null!;
    public Quotation Quotation { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public TechnicianProfile Technician { get; set; } = null!;
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<ScopeChangeRequest> ScopeChanges { get; set; } = [];
}
