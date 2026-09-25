using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class RequestInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Sent;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public ServiceRequest Request { get; set; } = null!;
    public TechnicianProfile Technician { get; set; } = null!;
}
