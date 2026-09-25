using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class CategoryVerificationRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public EvidenceType EvidenceType { get; set; }
    public bool IsRequired { get; set; } = true;
    public ValidationMethod ValidationMethod { get; set; } = ValidationMethod.Manual;
    public string RuleVersion { get; set; } = "1.0";

    public ServiceCategory Category { get; set; } = null!;
}
