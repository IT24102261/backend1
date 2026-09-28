using System.Text.Json;
using System.Text.Json.Nodes;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Interfaces;
using Microsoft.Extensions.Options;
using FixFlow.Application.Agents;

namespace FixFlow.Infrastructure.ExternalServices;

public sealed class DeterministicAiModelClient(IOptions<AiOptions> options) : IAiModelClient, ILlmModelAdapter
{
    public Task<string> CompleteJsonAsync(string prompt, CancellationToken cancellationToken = default) =>
        CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = "Legacy",
            Objective = AgentSafety.SanitizeUserText(prompt, 500),
            StructuredInputJson = JsonSerializer.Serialize(new { prompt = AgentSafety.SanitizeUserText(prompt) }, AgentJson.Options),
            MaxOutputChars = options.Value.MaxOutputChars
        }, cancellationToken);

    public Task<string> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(request.StructuredInputJson) ? "{}" : request.StructuredInputJson) as JsonObject ?? [];
        var max = Math.Max(256, request.MaxOutputChars);
        var json = request.AgentName switch
        {
            nameof(RequestPlanningAgent) => Plan(node),
            nameof(TechnicianMatchingAgent) => Match(node),
            nameof(QuotationRecommendationAgent) => Recommend(node),
            nameof(BookingValidationAgent) => Validate(node),
            _ => JsonSerializer.Serialize(new { status = "ok", agent = request.AgentName }, AgentJson.Options)
        };

        if (json.Length > max)
        {
            json = json[..max];
        }

        return Task.FromResult(json);
    }

    private static string Plan(JsonObject node)
    {
        var text = node["untrustedCustomerText"]?.ToString() ?? node["prompt"]?.ToString() ?? string.Empty;
        if (AgentSafety.IsPrimarilyInjection(text))
        {
            return JsonSerializer.Serialize(new PlanningOutput
            {
                ClarificationRequired = true,
                ClarificationQuestions = ["The submitted text could not be used as a service request."],
                Plan = ["Reject unsafe instruction text"]
            }, AgentJson.Options);
        }

        var optional = node["optionalCategory"]?.ToString();
        var category = optional switch
        {
            { } value when value.Equals("Electrical", StringComparison.OrdinalIgnoreCase) => "Electrician",
            { Length: > 0 } value => value,
            _ => InferCategory(text)
        };
        var subcategory = InferSubcategory(text, category);
        var dangerous = AgentSafety.IsDangerousElectrical(text);
        var missingArea = string.IsNullOrWhiteSpace(node["serviceArea"]?.ToString());
        var questions = new List<string>();
        if (dangerous)
        {
            questions.Add("A licensed electrician must handle this. Confirm you want a professional visit and do not attempt live repairs.");
        }
        if (missingArea)
        {
            questions.Add("Which city or district should the technician visit?");
        }
        if (string.IsNullOrWhiteSpace(category))
        {
            questions.Add("Which service category do you need?");
        }

        var quantity = InferQuantity(text);
        var needsCount = NeedsChangeCount(text, category, quantity);
        if (needsCount)
        {
            questions.Add("How many need to be changed?");
        }

        var missing = new List<string>();
        if (missingArea) missing.Add("service area");
        if (needsCount) missing.Add("quantity to change");
        if (string.IsNullOrWhiteSpace(category)) missing.Add("service category");

        return JsonSerializer.Serialize(new PlanningOutput
        {
            Category = category,
            Subcategory = subcategory,
            RequiredTechnician = string.IsNullOrWhiteSpace(category) ? string.Empty : category,
            Quantity = quantity,
            Confidence = category == "Electrician" ? 0.91 : 0.72,
            ClarificationRequired = dangerous || missingArea || string.IsNullOrWhiteSpace(category) || needsCount,
            ClarificationQuestions = questions,
            MissingInformation = missing,
            Plan =
            [
                "Find eligible verified technicians",
                "Request quotations",
                "Collect and validate quotations",
                "Compare suitable technicians",
                "Request customer approval",
                "Validate and confirm the booking"
            ]
        }, AgentJson.Options);
    }

    private static string Match(JsonObject node)
    {
        var eligible = node["eligible"]?.Deserialize<List<EligibleTechnician>>(AgentJson.Options) ?? [];
        var excluded = node["excluded"]?.Deserialize<List<ExcludedTechnician>>(AgentJson.Options) ?? [];
        return JsonSerializer.Serialize(new MatchingOutput
        {
            EligibleTechnicians = eligible,
            ExcludedTechnicians = excluded
        }, AgentJson.Options);
    }

    private static string Recommend(JsonObject node)
    {
        var ids = node["validQuotationIds"]?.AsArray().Select(x => Guid.TryParse(x?.ToString(), out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToList() ?? [];
        return JsonSerializer.Serialize(new RecommendationOutput
        {
            Options = ids.Select(id => new QuoteOption
            {
                QuotationId = id,
                Strengths = ["Submitted by a verified technician", "Real quotation stored by the API"],
                Tradeoffs = ["Customer must compare totals and arrival themselves"],
                Summary = "Eligible quotation. This is not an automatic booking."
            }).ToList()
        }, AgentJson.Options);
    }

    private static string Validate(JsonObject node)
    {
        var errors = node["errors"]?.AsArray().Select(x => x?.ToString() ?? string.Empty).Where(x => x.Length > 0).ToList() ?? [];
        return JsonSerializer.Serialize(new ValidationOutput
        {
            Valid = errors.Count == 0,
            BookingAllowed = errors.Count == 0,
            Errors = errors
        }, AgentJson.Options);
    }

    private static string InferCategory(string text)
    {
        var value = text.ToLowerInvariant();
        if (value.Contains("switch") || value.Contains("plug") || value.Contains("socket") || value.Contains("outlet") || value.Contains("electric") || value.Contains("wire") || value.Contains("bulb"))
        {
            return "Electrician";
        }

        if (value.Contains("leak") || value.Contains("tap") || value.Contains("plumb"))
        {
            return "Plumber";
        }

        if (value.Contains("ac") || value.Contains("air con") || value.Contains("fridge") || value.Contains("refrigerat"))
        {
            return "AC/Refrigeration";
        }

        if (value.Contains("carpenter") || value.Contains("wood") || value.Contains("door") || value.Contains("furniture"))
        {
            return "Carpenter";
        }

        if (value.Contains("paint"))
        {
            return "Painter";
        }

        if (value.Contains("solar") || value.Contains("panel"))
        {
            return "Solar";
        }

        if (value.Contains("washer") || value.Contains("washing") || value.Contains("appliance") || value.Contains("oven"))
        {
            return "Appliance Repair";
        }

        return string.Empty;
    }

    private static string InferSubcategory(string text, string category)
    {
        var value = text.ToLowerInvariant();
        if (category == "Electrician" && value.Contains("switch") && (value.Contains("replac") || value.Contains("not working") || value.Contains("damaged")))
        {
            return "Switch Replacement";
        }

        return category == "Electrician" ? "Electrical Repair" : "General";
    }

    private static bool NeedsChangeCount(string text, string category, int? quantity)
    {
        if (quantity is not null || text.Any(char.IsDigit))
        {
            return false;
        }

        var value = text.ToLowerInvariant();
        string[] counts = ["one", "two", "three", "four", "five", "several", "multiple"];
        if (counts.Any(value.Contains))
        {
            return false;
        }

        string[] fittings = ["switch", "plug", "socket", "outlet", "bulb", "light", "fan", "breaker", "tap", "faucet"];
        if (!fittings.Any(value.Contains))
        {
            return false;
        }

        return category is "Electrician" or "Electrical" or "Plumber" || string.IsNullOrWhiteSpace(category);
    }

    private static int? InferQuantity(string text)
    {
        var value = text.ToLowerInvariant();
        if (value.Contains("two") || value.Contains(" 2 ") || value.StartsWith("2 ") || value.Contains("2 switch"))
        {
            return 2;
        }

        if (value.Contains("three") || value.Contains("3 switch"))
        {
            return 3;
        }

        var digit = System.Text.RegularExpressions.Regex.Match(value, @"\b(\d+)\b");
        if (digit.Success && int.TryParse(digit.Groups[1].Value, out var parsed) && parsed is > 0 and < 100)
        {
            return parsed;
        }

        return null;
    }
}
