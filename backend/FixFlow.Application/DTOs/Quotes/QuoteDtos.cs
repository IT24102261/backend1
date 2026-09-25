namespace FixFlow.Application.DTOs.Quotes;

public class QuoteWriteRequest
{
    public decimal LabourAmount { get; set; }
    public decimal MaterialsAmount { get; set; }
    public decimal TravelAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "LKR";
    public int? DurationMinutes { get; set; }
    public DateTimeOffset? ArrivalStart { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Assumptions { get; set; }
    public string? IncludedMaterials { get; set; }
    public string? ExcludedMaterials { get; set; }
}

public class QuoteDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid QuoteGroupId { get; set; }
    public decimal LabourAmount { get; set; }
    public decimal MaterialsAmount { get; set; }
    public decimal TravelAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "LKR";
    public int DurationMinutes { get; set; }
    public DateTimeOffset? ArrivalStart { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Assumptions { get; set; }
    public string? IncludedMaterials { get; set; }
    public string? ExcludedMaterials { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public double? ApproximateDistanceKm { get; set; }
    public bool DistanceUnavailable { get; set; }
    public string? DistanceBand { get; set; }
    public string? TechnicianDisplayName { get; set; }
    public decimal? AverageRating { get; set; }
    public int? ReviewCount { get; set; }
    public int? CompletedJobs { get; set; }
    public string? RecommendationSummary { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Tradeoffs { get; set; } = [];
    public string? ProfilePhotoUrl { get; set; }
}

public class InvitationDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public string? ServiceArea { get; set; }
    public string? CategoryName { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ConfirmBookingRequest
{
    public Guid BookingId { get; set; }
}

public class BookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? AddressReleaseAt { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? TechnicianDisplayName { get; set; }
    public string? CustomerDisplayName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CategoryName { get; set; }
    public string? RequestDescription { get; set; }
    public string? ServiceArea { get; set; }
    public DateTimeOffset? PreferredStart { get; set; }
    public decimal? QuoteTotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public int Version { get; set; }
}

public class BookingHistoryDto
{
    public Guid Id { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ScopeChangeWriteRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal ProposedCost { get; set; }
}

public class ScopeChangeDecisionRequest
{
    public string Decision { get; set; } = string.Empty;
}

public class ScopeChangeDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal ProposedCost { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CustomerDecision { get; set; } = string.Empty;
    public DateTimeOffset? DecisionAt { get; set; }
}
