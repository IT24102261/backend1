namespace FixFlow.Application.DTOs.Technicians;

public class TechnicianProfileRequest
{
    public string? Bio { get; set; }
    public string? ServiceArea { get; set; }
    public double? LatitudeApprox { get; set; }
    public double? LongitudeApprox { get; set; }
    public string? ExperienceSummary { get; set; }
}

public class TechnicianProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ServiceArea { get; set; }
    public double? LatitudeApprox { get; set; }
    public double? LongitudeApprox { get; set; }
    public string? ExperienceSummary { get; set; }
    public bool IsSuspended { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public IReadOnlyList<string> ApprovedCategories { get; set; } = [];
    public int CompletedJobs { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class PublicTechnicianDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<string> ApprovedCategories { get; set; } = [];
    public bool CategoryVerified { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedJobs { get; set; }
    public string? ServiceSummary { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class TechnicianApplicationRequest
{
    public Guid CategoryId { get; set; }
}

public class TechnicianApplicationDto
{
    public Guid Id { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }
    public int Version { get; set; }
}

public class ApplicationDecisionRequest
{
    public string? Notes { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed record TechnicianPhotoFile(Stream Content, string ContentType);
