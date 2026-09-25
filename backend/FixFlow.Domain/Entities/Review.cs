using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public int Rating { get; set; }
    public string? Body { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ModerationReason { get; set; }
    public string? AdminReply { get; set; }
    public DateTimeOffset? AdminRepliedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public TechnicianProfile Technician { get; set; } = null!;
}
