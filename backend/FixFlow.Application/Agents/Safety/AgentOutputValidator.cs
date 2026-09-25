using FixFlow.Application.Agents.Contracts;
using FixFlow.Domain.Entities;

namespace FixFlow.Application.Agents.Safety;

public static class AgentOutputValidator
{
    public static PlanningOutput ValidatePlanning(PlanningOutput output, IReadOnlyList<ServiceCategory> catalog)
    {
        output.Category = output.Category?.Trim() ?? string.Empty;
        output.Subcategory = output.Subcategory?.Trim() ?? string.Empty;
        output.ClarificationQuestions = output.ClarificationQuestions?
            .Select(AgentSafety.SanitizeUserText)
            .Where(x => x.Length > 0)
            .Take(6)
            .ToList() ?? [];
        output.Plan = output.Plan?.Select(AgentSafety.SanitizeUserText).Where(x => x.Length > 0).Take(12).ToList() ?? [];
        output.MissingInformation = output.MissingInformation?.Select(AgentSafety.SanitizeUserText).Where(x => x.Length > 0).Take(6).ToList() ?? [];
        output.RequiredTechnician = AgentSafety.SanitizeUserText(output.RequiredTechnician, 80);
        output.Confidence = Math.Clamp(output.Confidence, 0, 1);

        if (output.Category.Equals("Electrical", StringComparison.OrdinalIgnoreCase))
        {
            output.Category = "Electrician";
        }

        var match = catalog.FirstOrDefault(x => x.IsActive && x.Name.Equals(output.Category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(output.Category) && match is null)
        {
            throw new AgentOutputException("INVENTED_CATEGORY", "The model invented a category that is not in the catalog.");
        }

        if (match is not null)
        {
            output.Category = match.Name;
            if (string.IsNullOrWhiteSpace(output.RequiredTechnician))
            {
                output.RequiredTechnician = match.Name;
            }
        }

        if (output.ClarificationRequired && output.ClarificationQuestions.Count == 0)
        {
            output.ClarificationQuestions.Add("Please add the missing details so matching can continue.");
        }

        if (output.Plan.Count == 0)
        {
            output.Plan =
            [
                "Find eligible verified technicians",
                "Request quotations",
                "Collect and validate quotations",
                "Compare suitable technicians",
                "Request customer approval",
                "Validate and confirm the booking"
            ];
        }

        return output;
    }

    public static RecommendationOutput ValidateRecommendation(RecommendationOutput output, IReadOnlyCollection<Guid> validQuoteIds)
    {
        output.Options = (output.Options ?? [])
            .Where(x => validQuoteIds.Contains(x.QuotationId))
            .Select(x => new QuoteOption
            {
                QuotationId = x.QuotationId,
                Strengths = x.Strengths?.Take(6).ToList() ?? [],
                Tradeoffs = x.Tradeoffs?.Take(6).ToList() ?? [],
                Summary = AgentSafety.SanitizeUserText(x.Summary, 500)
            })
            .ToList();
        return output;
    }

    public static ValidationOutput ValidateValidation(ValidationOutput output)
    {
        output.Errors = output.Errors?.Select(AgentSafety.SanitizeUserText).Where(x => x.Length > 0).ToList() ?? [];
        if (output.Errors.Count > 0)
        {
            output.Valid = false;
            output.BookingAllowed = false;
        }

        if (!output.Valid)
        {
            output.BookingAllowed = false;
        }

        return output;
    }
}
