using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class VerificationCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Guid? EvidenceId { get; set; }
    public VerificationSourceType SourceType { get; set; }
    public string? SourceReference { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid AdminId { get; set; }
    public VerificationOutcome Outcome { get; set; }
    public string? Notes { get; set; }

    public TechnicianCategoryApplication Application { get; set; } = null!;
    public TechnicianDocument? Evidence { get; set; }
    public User Admin { get; set; } = null!;
}
