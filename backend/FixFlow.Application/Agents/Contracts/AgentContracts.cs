using System.Text.Json.Serialization;

namespace FixFlow.Application.Agents.Contracts;

public class PlanningInput
{
    public Guid RequestId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OptionalCategory { get; set; }
    public string? ServiceArea { get; set; }
    public DateTimeOffset? PreferredStart { get; set; }
    public DateTimeOffset? PreferredEnd { get; set; }
    public IReadOnlyList<ImageMetadata> Images { get; set; } = [];
}

public class ImageMetadata
{
    public Guid Id { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}

public class PlanningOutput
{
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string RequiredTechnician { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public double Confidence { get; set; }
    public bool ClarificationRequired { get; set; }
    public List<string> ClarificationQuestions { get; set; } = [];
    public List<string> MissingInformation { get; set; } = [];
    public List<string> Plan { get; set; } = [];
}

public class MatchingInput
{
    public Guid RequestId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ServiceArea { get; set; }
    public DateTimeOffset? PreferredStart { get; set; }
    public DateTimeOffset? PreferredEnd { get; set; }
}

public class MatchingOutput
{
    public List<EligibleTechnician> EligibleTechnicians { get; set; } = [];
    public List<ExcludedTechnician> ExcludedTechnicians { get; set; } = [];
}

public class EligibleTechnician
{
    public Guid TechnicianId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ServiceArea { get; set; }
}

public class ExcludedTechnician
{
    public Guid TechnicianId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RecommendationInput
{
    public Guid RequestId { get; set; }
}

public class RecommendationOutput
{
    public List<QuoteOption> Options { get; set; } = [];
}

public class QuoteOption
{
    public Guid QuotationId { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Tradeoffs { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}

public class ValidationInput
{
    public Guid RequestId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? WorkflowId { get; set; }
}

public class ValidationOutput
{
    public bool Valid { get; set; }
    public bool BookingAllowed { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class WorkflowRunResult
{
    public Guid WorkflowId { get; set; }
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string PlanJson { get; set; } = "{}";
    public string? OutcomeJson { get; set; }
    public string? ErrorCode { get; set; }
    public bool ClarificationRequired { get; set; }
    public ValidationOutput? Validation { get; set; }
}

public static class AgentJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
