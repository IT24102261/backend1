namespace FixFlow.Domain.Entities;

public class TechnicianProfileImage
{
    public Guid TechnicianId { get; set; }
    public byte[] Content { get; set; } = [];
    public string MimeType { get; set; } = "image/jpeg";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public TechnicianProfile Technician { get; set; } = null!;
}
