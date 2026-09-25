namespace FixFlow.Application.DTOs.Categories;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<CategoryRequirementDto> Requirements { get; set; } = [];
}

public class CategoryRequirementDto
{
    public Guid Id { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string ValidationMethod { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = "1.0";
}

public class CategoryRequirementWriteRequest
{
    public string EvidenceType { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string ValidationMethod { get; set; } = "MANUAL";
    public string RuleVersion { get; set; } = "1.0";
}

public class CategoryWriteRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
}
