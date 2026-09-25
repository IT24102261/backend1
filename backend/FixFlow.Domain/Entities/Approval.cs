using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class Approval
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? RequestId { get; set; }
    public Guid ActorId { get; set; }
    public ApprovalDecision Decision { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }

    public AiWorkflow Workflow { get; set; } = null!;
    public Booking? Booking { get; set; }
    public ServiceRequest? Request { get; set; }
    public User Actor { get; set; } = null!;
}
