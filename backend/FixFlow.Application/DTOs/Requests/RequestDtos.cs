namespace FixFlow.Application.DTOs.Requests;

public class RequestWriteRequest
{
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? PreferredStart { get; set; }
    public DateTimeOffset? PreferredEnd { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? ServiceArea { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class ClarificationRequest
{
    public string Message { get; set; } = string.Empty;
}

public class RequestDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset? PreferredStart { get; set; }
    public DateTimeOffset? PreferredEnd { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? ServiceArea { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class RequestHistoryDto
{
    public Guid Id { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? Note { get; set; }
}

public class MediaDto
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
