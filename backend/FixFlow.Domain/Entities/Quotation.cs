using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid? InvitationId { get; set; }
    public Guid QuoteGroupId { get; set; } = Guid.NewGuid();
    public decimal LabourAmount { get; set; }
    public decimal MaterialsAmount { get; set; }
    public decimal TravelAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "LKR";
    public DateTimeOffset? ArrivalStart { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Assumptions { get; set; }
    public string? IncludedMaterials { get; set; }
    public string? ExcludedMaterials { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ServiceRequest Request { get; set; } = null!;
    public TechnicianProfile Technician { get; set; } = null!;
    public RequestInvitation? Invitation { get; set; }
}
