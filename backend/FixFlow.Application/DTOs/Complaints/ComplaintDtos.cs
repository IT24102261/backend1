namespace FixFlow.Application.DTOs.Complaints;

public class ComplaintWriteRequest
{
    public Guid BookingId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ComplaintDto
{
    public Guid Id { get; set; }
    public Guid? BookingId { get; set; }
    public Guid ReportedById { get; set; }
    public string? TechnicianDisplayName { get; set; }
    public string? CustomerDisplayName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public string? AdminReply { get; set; }
    public DateTimeOffset? AdminRepliedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public class ComplaintStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
}
