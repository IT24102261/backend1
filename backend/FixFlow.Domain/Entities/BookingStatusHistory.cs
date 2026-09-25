using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class BookingStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid ActorId { get; set; }
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }

    public Booking Booking { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
