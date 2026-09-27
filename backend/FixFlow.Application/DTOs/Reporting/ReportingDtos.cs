namespace FixFlow.Application.DTOs.Reporting;

public class DashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalTechnicians { get; set; }
    public int PendingVerification { get; set; }
    public int ApprovedTechnicians { get; set; }
    public int RejectedApplications { get; set; }
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int CompletedJobs { get; set; }
    public int CancelledJobs { get; set; }
    public int Technicians { get; set; }
    public int PendingApplications { get; set; }
    public int OpenComplaints { get; set; }
    public int AiWorkflows { get; set; }
    public decimal AverageRating { get; set; }
    public double AiWorkflowSuccessRate { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? MetadataSummary { get; set; }
}

public class RequestReportDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class BookingReportDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid TechnicianId { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public class TechnicianReportDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsSuspended { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class AgentReportDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
}
