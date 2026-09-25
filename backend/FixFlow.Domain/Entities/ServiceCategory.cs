namespace FixFlow.Domain.Entities;

public class ServiceCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;

    public ServiceCategory? Parent { get; set; }
    public ICollection<ServiceCategory> Children { get; set; } = [];
    public ICollection<CategoryVerificationRequirement> VerificationRequirements { get; set; } = [];
}
