namespace FixFlow.Application.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? TechnicianProfileId { get; set; }
    public string? Address { get; set; }
    public string? ServiceArea { get; set; }
    public string? RequestedCategory { get; set; }
    public string LoginStatus { get; set; } = "ACTIVE";
}

public class AdminCreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "CUSTOMER";
    public string? Address { get; set; }
    public string? ServiceArea { get; set; }
    public Guid? CategoryId { get; set; }
}

public class AdminUserStatusRequest
{
    public bool IsActive { get; set; }
}

public class AdminReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}

public class AdminReviewUpdateRequest
{
    public int Rating { get; set; }
    public string? Body { get; set; }
}
