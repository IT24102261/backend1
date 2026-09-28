using System.Text.Json;
using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Domain.Rules;

namespace FixFlow.Application.Agents.Tools;

public abstract class AgentToolBase : IAgentTool
{
    public abstract string Name { get; }
    public abstract string Purpose { get; }
    public abstract AgentRole AuthorizedAgent { get; }
    public abstract string InputSchema { get; }
    public abstract string OutputSchema { get; }
    public virtual TimeSpan Timeout => TimeSpan.FromSeconds(8);
    public abstract Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken);

    protected static bool TryGuid(JsonElement input, string name, out Guid value)
    {
        value = Guid.Empty;
        if (!input.TryGetProperty(name, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.String
            ? Guid.TryParse(property.GetString(), out value)
            : property.TryGetGuid(out value);
    }

    protected static string? ReadString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

public sealed class GetServiceCategoriesTool(IRepository<ServiceCategory> categories) : AgentToolBase
{
    public override string Name => "GetServiceCategories";
    public override string Purpose => "Return active catalog categories. Agents must not invent names.";
    public override AgentRole AuthorizedAgent => AgentRole.RequestPlanning;
    public override string InputSchema => """{"type":"object"}""";
    public override string OutputSchema => """{"categories":[{"id":"guid","name":"string"}]}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        var items = await categories.Query().Where(x => x.IsActive).OrderBy(x => x.Name).ToMaterializedListAsync(cancellationToken);
        return AgentToolResult.Success(new { categories = items.Select(x => new { x.Id, x.Name, x.Description }) });
    }
}

public sealed class GetRequestTool(IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "GetRequest";
    public override string Purpose => "Load a service request by ID.";
    public override AgentRole AuthorizedAgent => AgentRole.RequestPlanning;
    public override string InputSchema => """{"requestId":"guid"}""";
    public override string OutputSchema => """{"id":"guid","description":"string","categoryId":"guid?"}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId is required.");
        }

        var request = await requests.GetByIdAsync(id, cancellationToken);
        return request is null
            ? AgentToolResult.Fail("NOT_FOUND", "Request not found.")
            : AgentToolResult.Success(new
            {
                request.Id,
                request.CustomerId,
                request.CategoryId,
                request.Description,
                request.ServiceArea,
                request.PreferredStart,
                request.PreferredEnd,
                Status = EnumMap.ToApi(request.Status)
            });
    }
}

public sealed class GetCategoryRequirementsTool(IRepository<CategoryVerificationRequirement> requirements) : AgentToolBase
{
    public override string Name => "GetCategoryRequirements";
    public override string Purpose => "Return verification requirements for a catalog category.";
    public override AgentRole AuthorizedAgent => AgentRole.RequestPlanning;
    public override string InputSchema => """{"categoryId":"guid"}""";
    public override string OutputSchema => """{"requirements":[{"evidenceType":"string","isRequired":true}]}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "categoryId", out var categoryId))
        {
            return AgentToolResult.Fail("INVALID_ID", "categoryId is required.");
        }

        var items = await requirements.Query().Where(x => x.CategoryId == categoryId).ToMaterializedListAsync(cancellationToken);
        var payload = items.Select(x => new { evidenceType = EnumMap.ToApi(x.EvidenceType), x.IsRequired, x.RuleVersion }).ToList();
        if (payload.Count == 0)
        {
            payload.Add(new { evidenceType = "LICENSE", IsRequired = true, RuleVersion = "catalog-default" });
        }

        return AgentToolResult.Success(new { requirements = payload });
    }
}

public sealed class GetApprovedTechniciansByCategoryTool(
    IRepository<TechnicianCategoryApplication> applications,
    IRepository<TechnicianProfile> technicians,
    IRepository<User> users) : AgentToolBase
{
    public override string Name => "GetApprovedTechniciansByCategory";
    public override string Purpose => "List technicians with an approved category application. Registration is not verification.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"categoryId":"guid"}""";
    public override string OutputSchema => """{"technicians":[{"technicianId":"guid","approved":true}]}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "categoryId", out var categoryId))
        {
            return AgentToolResult.Fail("INVALID_ID", "categoryId is required.");
        }

        var apps = await applications.Query()
            .Where(x => x.CategoryId == categoryId && x.Status == ApplicationStatus.Approved)
            .ToMaterializedListAsync(cancellationToken);

        var rows = new List<object>();
        foreach (var application in apps)
        {
            var technician = await technicians.GetByIdAsync(application.TechnicianId, cancellationToken);
            if (technician is null)
            {
                continue;
            }

            var user = await users.GetByIdAsync(technician.UserId, cancellationToken);
            rows.Add(new
            {
                technicianId = technician.Id,
                userId = technician.UserId,
                displayName = user?.DisplayName ?? string.Empty,
                approved = true,
                accountActive = user?.IsActive == true,
                suspended = technician.IsSuspended,
                serviceArea = technician.ServiceArea
            });
        }

        return AgentToolResult.Success(new { technicians = rows });
    }
}

public sealed class CheckTechnicianActiveTool(IRepository<TechnicianProfile> technicians, IRepository<User> users) : AgentToolBase
{
    public override string Name => "CheckTechnicianActive";
    public override string Purpose => "Confirm the technician account is active and not suspended.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"technicianId":"guid"}""";
    public override string OutputSchema => """{"active":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        var technician = await technicians.GetByIdAsync(id, cancellationToken);
        if (technician is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Technician not found.");
        }

        var user = await users.GetByIdAsync(technician.UserId, cancellationToken);
        var active = user is { IsActive: true } && !technician.IsSuspended;
        return AgentToolResult.Success(new { technicianId = id, active, accountActive = user?.IsActive == true, suspended = technician.IsSuspended });
    }
}

public sealed class CheckServiceAreaTool(IRepository<TechnicianProfile> technicians, IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "CheckServiceArea";
    public override string Purpose => "Compare technician service area with the request area.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"technicianId":"guid","requestId":"guid"}""";
    public override string OutputSchema => """{"matches":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var technicianId) || !TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId and requestId are required.");
        }

        var technician = await technicians.GetByIdAsync(technicianId, cancellationToken);
        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        if (technician is null || request is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Technician or request was not found.");
        }

        var matches = AreaMatches(request.ServiceArea, technician.ServiceArea);
        return AgentToolResult.Success(new { matches, requestArea = request.ServiceArea, technicianArea = technician.ServiceArea });
    }

    public static bool AreaMatches(string? requestArea, string? technicianArea)
    {
        if (string.IsNullOrWhiteSpace(requestArea) || string.IsNullOrWhiteSpace(technicianArea))
        {
            return false;
        }

        var left = requestArea.Trim();
        var right = technicianArea.Trim();
        if (left.Contains(right, StringComparison.OrdinalIgnoreCase)
            || right.Contains(left, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return DistrictGroups.Any(group => ContainsAny(left, group) && ContainsAny(right, group));
    }

    private static readonly string[][] DistrictGroups =
    [
        ["jaffna", "nallur", "chundikuli", "gurunagar", "kopay", "chunnakam", "manipay", "tellippalai", "karainagar", "point pedro"],
        ["colombo", "nugegoda", "dehiwala", "kotte", "mount lavinia"],
        ["kandy", "peradeniya", "katugastota"],
        ["negombo", "kochchikade", "katana", "wennappuwa", "seeduwa"]
    ];

    private static bool ContainsAny(string value, IEnumerable<string> tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}

public sealed class CheckAvailabilityTool(IRepository<Booking> bookings, IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "CheckAvailability";
    public override string Purpose => "Check whether the technician is free in the preferred window.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"technicianId":"guid","preferredStart":"datetime?","preferredEnd":"datetime?"}""";
    public override string OutputSchema => """{"available":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var technicianId))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        DateTimeOffset? start = input.TryGetProperty("preferredStart", out var startEl) && startEl.ValueKind == JsonValueKind.String
            ? DateTimeOffset.TryParse(startEl.GetString(), out var parsedStart) ? parsedStart : null
            : null;
        DateTimeOffset? end = input.TryGetProperty("preferredEnd", out var endEl) && endEl.ValueKind == JsonValueKind.String
            ? DateTimeOffset.TryParse(endEl.GetString(), out var parsedEnd) ? parsedEnd : null
            : null;

        var available = !await HasConflict(bookings, quotations, technicianId, start, end, null, cancellationToken);
        return AgentToolResult.Success(new { technicianId, available });
    }

    public static async Task<bool> HasConflict(
        IRepository<Booking> bookings,
        IRepository<Quotation> quotations,
        Guid technicianId,
        DateTimeOffset? start,
        DateTimeOffset? end,
        Guid? ignoreBookingId,
        CancellationToken cancellationToken)
    {
        if (start is null)
        {
            return false;
        }

        var windowEnd = end ?? start.Value.AddHours(2);
        var active = await bookings.Query()
            .Where(x => x.TechnicianId == technicianId && (ignoreBookingId == null || x.Id != ignoreBookingId))
            .ToMaterializedListAsync(cancellationToken);

        foreach (var booking in active.Where(x => BookingStateMachine.OccupiesTechnician(x.Status)))
        {
            var quote = await quotations.GetByIdAsync(booking.QuotationId, cancellationToken);
            if (quote?.ArrivalStart is null)
            {
                continue;
            }

            var existingStart = quote.ArrivalStart.Value;
            var existingEnd = existingStart.AddMinutes(Math.Max(quote.DurationMinutes, 30));
            if (existingStart < windowEnd && existingEnd > start)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class CheckCapacityTool(IRepository<Booking> bookings) : AgentToolBase
{
    public const int MaxActiveJobs = 8;
    public override string Name => "CheckCapacity";
    public override string Purpose => "Reject technicians already at active-job capacity.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"technicianId":"guid"}""";
    public override string OutputSchema => """{"withinCapacity":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var technicianId))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        var jobs = await bookings.Query().Where(x => x.TechnicianId == technicianId).ToMaterializedListAsync(cancellationToken);
        var active = jobs.Count(x => BookingStateMachine.OccupiesTechnician(x.Status));
        return AgentToolResult.Success(new { technicianId, active, withinCapacity = active < MaxActiveJobs });
    }
}

public sealed class GetSkillTagsTool(IRepository<TechnicianProfile> technicians) : AgentToolBase
{
    public override string Name => "GetSkillTags";
    public override string Purpose => "Return coarse skill tags from the technician profile text.";
    public override AgentRole AuthorizedAgent => AgentRole.TechnicianMatching;
    public override string InputSchema => """{"technicianId":"guid"}""";
    public override string OutputSchema => """{"tags":["string"]}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        var technician = await technicians.GetByIdAsync(id, cancellationToken);
        if (technician is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Technician not found.");
        }

        var blob = $"{technician.Bio} {technician.ExperienceSummary}".ToLowerInvariant();
        var tags = new[] { "electrical", "switch", "plumbing", "ac", "solar", "carpentry", "paint" }
            .Where(tag => blob.Contains(tag))
            .ToList();
        return AgentToolResult.Success(new { technicianId = id, tags });
    }
}

public sealed class GetValidQuotationsTool(IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "GetValidQuotations";
    public override string Purpose => "Return real, still-valid quotations for a request. Never invent rows.";
    public override AgentRole AuthorizedAgent => AgentRole.QuotationRecommendation;
    public override string InputSchema => """{"requestId":"guid"}""";
    public override string OutputSchema => """{"quotations":[{"id":"guid","totalAmount":0}]}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var items = await quotations.Query()
            .Where(x => x.RequestId == requestId && x.Status == QuotationStatus.Sent && x.ExpiresAt > now)
            .ToMaterializedListAsync(cancellationToken);

        return AgentToolResult.Success(new
        {
            quotations = items.Select(x => new
            {
                x.Id,
                x.TechnicianId,
                x.LabourAmount,
                x.MaterialsAmount,
                x.TravelAmount,
                x.TotalAmount,
                x.Currency,
                x.ArrivalStart,
                x.DurationMinutes,
                x.ExpiresAt,
                x.Assumptions
            })
        });
    }
}

public sealed class GetTechnicianReputationTool(IRepository<TechnicianProfile> technicians, IRepository<Review> reviews) : AgentToolBase
{
    public override string Name => "GetTechnicianReputation";
    public override string Purpose => "Return verified rating statistics only.";
    public override AgentRole AuthorizedAgent => AgentRole.QuotationRecommendation;
    public override string InputSchema => """{"technicianId":"guid"}""";
    public override string OutputSchema => """{"averageRating":0,"verifiedReviewCount":0}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        var technician = await technicians.GetByIdAsync(id, cancellationToken);
        var published = await reviews.Query()
            .Where(x => x.TechnicianId == id && x.Status == ReviewStatus.Published)
            .ToMaterializedListAsync(cancellationToken);
        return AgentToolResult.Success(new
        {
            technicianId = id,
            averageRating = technician?.AverageRating ?? 0,
            verifiedReviewCount = published.Count,
            storedReviewCount = technician?.ReviewCount ?? 0
        });
    }
}

public sealed class GetCompletedJobCountTool(IRepository<Booking> bookings) : AgentToolBase
{
    public override string Name => "GetCompletedJobCount";
    public override string Purpose => "Count completed jobs for a technician.";
    public override AgentRole AuthorizedAgent => AgentRole.QuotationRecommendation;
    public override string InputSchema => """{"technicianId":"guid"}""";
    public override string OutputSchema => """{"completedJobs":0}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId is required.");
        }

        var items = await bookings.Query().Where(x => x.TechnicianId == id).ToMaterializedListAsync(cancellationToken);
        var completed = items.Count(x => x.Status is BookingStatus.Closed or BookingStatus.CustomerConfirmed);
        return AgentToolResult.Success(new { technicianId = id, completedJobs = completed });
    }
}

public sealed class GetDistanceBandTool(
    IRepository<TechnicianProfile> technicians,
    IRepository<ServiceRequest> requests,
    IMapService maps) : AgentToolBase
{
    public override string Name => "GetDistanceBand";
    public override string Purpose => "Return a coarse distance band. Exact address stays hidden.";
    public override AgentRole AuthorizedAgent => AgentRole.QuotationRecommendation;
    public override string InputSchema => """{"technicianId":"guid","requestId":"guid"}""";
    public override string OutputSchema => """{"band":"LOCAL","distanceUnavailable":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var technicianId) || !TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId and requestId are required.");
        }

        var technician = await technicians.GetByIdAsync(technicianId, cancellationToken);
        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        if (technician is null || request is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Technician or request was not found.");
        }

        var distance = await maps.CalculateApproximateDistanceAsync(
            new DTOs.Maps.MapPoint
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                ServiceArea = request.ServiceArea
            },
            new DTOs.Maps.MapPoint
            {
                Latitude = technician.LatitudeApprox,
                Longitude = technician.LongitudeApprox,
                ServiceArea = technician.ServiceArea
            },
            cancellationToken);

        return AgentToolResult.Success(new
        {
            band = distance.DistanceUnavailable ? null : distance.Band,
            distanceKm = distance.DistanceUnavailable ? null : distance.DistanceKm,
            distanceUnavailable = distance.DistanceUnavailable,
            requestArea = request.ServiceArea,
            technicianArea = technician.ServiceArea
        });
    }
}

public sealed class GetCustomerPreferencesTool(IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "GetCustomerPreferences";
    public override string Purpose => "Return coarse customer scheduling preferences from the request.";
    public override AgentRole AuthorizedAgent => AgentRole.QuotationRecommendation;
    public override string InputSchema => """{"requestId":"guid"}""";
    public override string OutputSchema => """{"preferredStart":"datetime?"}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId is required.");
        }

        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        return request is null
            ? AgentToolResult.Fail("NOT_FOUND", "Request not found.")
            : AgentToolResult.Success(new { request.PreferredStart, request.PreferredEnd, request.ServiceArea });
    }
}

public sealed class GetQuotationTool(IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "GetQuotation";
    public override string Purpose => "Load one real quotation. Unknown IDs fail.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"quotationId":"guid"}""";
    public override string OutputSchema => """{"id":"guid","totalAmount":0}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "quotationId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "quotationId is required.");
        }

        var quote = await quotations.GetByIdAsync(id, cancellationToken);
        return quote is null
            ? AgentToolResult.Fail("NOT_FOUND", "Quotation not found.")
            : AgentToolResult.Success(new
            {
                quote.Id,
                quote.RequestId,
                quote.TechnicianId,
                quote.TotalAmount,
                quote.LabourAmount,
                quote.MaterialsAmount,
                quote.TravelAmount,
                quote.ExpiresAt,
                Status = EnumMap.ToApi(quote.Status)
            });
    }
}

public sealed class CheckQuoteExpiryTool(IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "CheckQuoteExpiry";
    public override string Purpose => "Reject expired quotations.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"quotationId":"guid"}""";
    public override string OutputSchema => """{"expired":false}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "quotationId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "quotationId is required.");
        }

        var quote = await quotations.GetByIdAsync(id, cancellationToken);
        if (quote is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Quotation not found.");
        }

        var expired = quote.ExpiresAt <= DateTimeOffset.UtcNow || quote.Status == QuotationStatus.Expired;
        return AgentToolResult.Success(new { quotationId = id, expired, quote.ExpiresAt });
    }
}

public sealed class CheckCustomerOwnershipTool(IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "CheckCustomerOwnership";
    public override string Purpose => "Confirm the acting user owns the request.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"requestId":"guid","customerId":"guid"}""";
    public override string OutputSchema => """{"owns":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var requestId) || !TryGuid(input, "customerId", out var customerId))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId and customerId are required.");
        }

        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        return request is null
            ? AgentToolResult.Fail("NOT_FOUND", "Request not found.")
            : AgentToolResult.Success(new { owns = request.CustomerId == customerId, request.CustomerId });
    }
}

public sealed class CheckCategoryApprovalTool(IRepository<TechnicianCategoryApplication> applications, IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "CheckCategoryApproval";
    public override string Purpose => "Confirm category-specific approval. Registration is never enough.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"technicianId":"guid","requestId":"guid"}""";
    public override string OutputSchema => """{"approved":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "technicianId", out var technicianId) || !TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "technicianId and requestId are required.");
        }

        var request = await requests.GetByIdAsync(requestId, cancellationToken);
        if (request?.CategoryId is null)
        {
            return AgentToolResult.Success(new { approved = false, reason = "Request has no category." });
        }

        var match = await applications.Query()
            .Where(x => x.TechnicianId == technicianId && x.CategoryId == request.CategoryId && x.Status == ApplicationStatus.Approved)
            .AnyMaterializedAsync(cancellationToken);
        return AgentToolResult.Success(new { approved = match });
    }
}

public sealed class CheckTechnicianAvailabilityTool(
    IRepository<Booking> bookings,
    IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "CheckTechnicianAvailability";
    public override string Purpose => "Re-check technician availability at booking time.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"quotationId":"guid"}""";
    public override string OutputSchema => """{"available":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "quotationId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "quotationId is required.");
        }

        var quote = await quotations.GetByIdAsync(id, cancellationToken);
        if (quote is null)
        {
            return AgentToolResult.Fail("NOT_FOUND", "Quotation not found.");
        }

        var conflict = await CheckAvailabilityTool.HasConflict(
            bookings, quotations, quote.TechnicianId, quote.ArrivalStart, quote.ArrivalStart?.AddMinutes(quote.DurationMinutes), null, cancellationToken);
        return AgentToolResult.Success(new { available = !conflict, quotationId = id });
    }
}

public sealed class CheckBookingConflictTool(IRepository<Booking> bookings) : AgentToolBase
{
    public override string Name => "CheckBookingConflict";
    public override string Purpose => "Reject a second active booking on the same request.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"requestId":"guid"}""";
    public override string OutputSchema => """{"conflict":false}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId is required.");
        }

        var items = await bookings.Query().Where(x => x.RequestId == requestId).ToMaterializedListAsync(cancellationToken);
        var conflict = items.Any(x => BookingStateMachine.IsActive(x.Status));
        return AgentToolResult.Success(new { conflict, requestId });
    }
}

public sealed class CheckQuoteRequestRelationshipTool(IRepository<Quotation> quotations) : AgentToolBase
{
    public override string Name => "CheckQuoteRequestRelationship";
    public override string Purpose => "Confirm the quotation belongs to the request.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"quotationId":"guid","requestId":"guid"}""";
    public override string OutputSchema => """{"matches":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "quotationId", out var quotationId) || !TryGuid(input, "requestId", out var requestId))
        {
            return AgentToolResult.Fail("INVALID_ID", "quotationId and requestId are required.");
        }

        var quote = await quotations.GetByIdAsync(quotationId, cancellationToken);
        return quote is null
            ? AgentToolResult.Fail("NOT_FOUND", "Quotation not found.")
            : AgentToolResult.Success(new { matches = quote.RequestId == requestId, quote.RequestId });
    }
}

public sealed class CheckApprovalTool(IRepository<Approval> approvals) : AgentToolBase
{
    public override string Name => "CheckApproval";
    public override string Purpose => "Confirm the owning customer has approved the booking action.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"workflowId":"guid","customerId":"guid"}""";
    public override string OutputSchema => """{"approved":true}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "workflowId", out var workflowId))
        {
            return AgentToolResult.Fail("INVALID_ID", "workflowId is required.");
        }

        TryGuid(input, "customerId", out var customerId);
        var items = await approvals.Query().Where(x => x.WorkflowId == workflowId).ToMaterializedListAsync(cancellationToken);
        var latest = items.OrderByDescending(x => x.Timestamp).FirstOrDefault();
        var approved = latest is { Decision: ApprovalDecision.Approved } && (customerId == Guid.Empty || latest.ActorId == customerId);
        return AgentToolResult.Success(new
        {
            approved,
            decision = latest is null ? null : EnumMap.ToApi(latest.Decision),
            actorId = latest?.ActorId
        });
    }
}

public sealed class GetRequestForValidationTool(IRepository<ServiceRequest> requests) : AgentToolBase
{
    public override string Name => "GetRequest";
    public override string Purpose => "Load the request during booking validation.";
    public override AgentRole AuthorizedAgent => AgentRole.BookingValidation;
    public override string InputSchema => """{"requestId":"guid"}""";
    public override string OutputSchema => """{"id":"guid","customerId":"guid"}""";

    public override async Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken)
    {
        if (!TryGuid(input, "requestId", out var id))
        {
            return AgentToolResult.Fail("INVALID_ID", "requestId is required.");
        }

        var request = await requests.GetByIdAsync(id, cancellationToken);
        return request is null
            ? AgentToolResult.Fail("NOT_FOUND", "Request not found.")
            : AgentToolResult.Success(new { request.Id, request.CustomerId, request.CategoryId, Status = EnumMap.ToApi(request.Status) });
    }
}
