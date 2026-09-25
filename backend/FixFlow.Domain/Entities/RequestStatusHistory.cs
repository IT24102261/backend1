using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class RequestStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid ActorId { get; set; }
    public ServiceRequestStatus FromStatus { get; set; }
    public ServiceRequestStatus ToStatus { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Note { get; set; }

    public ServiceRequest Request { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
