using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class Complaint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BookingId { get; set; }
    public Guid ReportedById { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public string? AdminReply { get; set; }
    public DateTimeOffset? AdminRepliedAt { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public Booking? Booking { get; set; }
    public User ReportedBy { get; set; } = null!;
}
