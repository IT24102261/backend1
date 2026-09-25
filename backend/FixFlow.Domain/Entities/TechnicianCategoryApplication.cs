using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class TechnicianCategoryApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TechnicianId { get; set; }
    public Guid CategoryId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAt { get; set; }
    public Guid? DecidedBy { get; set; }
    public string? DecisionNotes { get; set; }
    public int Version { get; set; } = 1;

    public TechnicianProfile Technician { get; set; } = null!;
    public ServiceCategory Category { get; set; } = null!;
    public User? DecisionMaker { get; set; }
    public ICollection<TechnicianDocument> Documents { get; set; } = [];
    public ICollection<VerificationCheck> VerificationChecks { get; set; } = [];
}
