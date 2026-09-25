using FixFlow.Domain.Enums;

namespace FixFlow.Domain.Entities;

public class ServiceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? PreferredStart { get; set; }
    public DateTimeOffset? PreferredEnd { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? ServiceArea { get; set; }
    public string? AddressEncrypted { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Customer { get; set; } = null!;
    public ServiceCategory? Category { get; set; }
    public ICollection<RequestMedia> Media { get; set; } = [];
    public ICollection<RequestInvitation> Invitations { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
    public ICollection<RequestStatusHistory> History { get; set; } = [];
    public ICollection<RequestClarification> Clarifications { get; set; } = [];
}
