namespace FixFlow.Application.DTOs.Reviews;

public class ReviewWriteRequest
{
    public int Rating { get; set; }
    public string? Body { get; set; }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public string? CustomerDisplayName { get; set; }
    public string? TechnicianDisplayName { get; set; }
    public int Rating { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ModerationReason { get; set; }
    public string? AdminReply { get; set; }
    public DateTimeOffset? AdminRepliedAt { get; set; }
}

public class ModerateReviewRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
