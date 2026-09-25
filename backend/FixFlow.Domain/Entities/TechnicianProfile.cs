namespace FixFlow.Domain.Entities;

public class TechnicianProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public string? ServiceArea { get; set; }
    public double? LatitudeApprox { get; set; }
    public double? LongitudeApprox { get; set; }
    public string? ExperienceSummary { get; set; }
    public string? ProfilePhotoStorageKey { get; set; }
    public string? ProfilePhotoMimeType { get; set; }
    public bool IsSuspended { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<TechnicianCategoryApplication> Applications { get; set; } = [];
}
