namespace FixFlow.Domain.Entities;

public class RequestClarification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid AuthorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ServiceRequest Request { get; set; } = null!;
    public User Author { get; set; } = null!;
}
