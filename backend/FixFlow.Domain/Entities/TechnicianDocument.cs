using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class TechnicianDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public EvidenceType EvidenceType { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string? ClaimNumberEncrypted { get; set; }
    public string? Issuer { get; set; }
    public DocumentReviewStatus ReviewStatus { get; set; } = DocumentReviewStatus.Pending;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public TechnicianCategoryApplication Application { get; set; } = null!;
}
