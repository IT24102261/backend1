namespace FixFlow.Infrastructure.ExternalServices;

public class MapOptions
{
    public const string SectionName = "Maps";

    public string Provider { get; set; } = "Nominatim";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public string? ApiKey { get; set; }
    public string UserAgent { get; set; } = "FixFlowAI/1.0 (university-project)";
    public int TimeoutSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 2;
    public int RetryDelayMilliseconds { get; set; } = 400;
}
