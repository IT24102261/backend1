using System.Text.Json;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Agents;

public class QuotationRecommendationAgent(IAiModelClient model, IAgentToolExecutor tools)
{
    public const AgentRole Role = AgentRole.QuotationRecommendation;

    public async Task<(RecommendationOutput Output, IReadOnlyList<string> ToolsUsed, IReadOnlyCollection<Guid> ValidQuoteIds)> RunAsync(
        RecommendationInput input,
        AgentToolContext context,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        var used = new List<string>();
        var quotes = await tools.CallAsync(
            context,
            "GetValidQuotations",
            JsonSerializer.Serialize(new { requestId = input.RequestId }, AgentJson.Options),
            cancellationToken);
        used.Add("GetValidQuotations");
        if (quotes.ErrorCode == "TOOL_TIMEOUT")
        {
            throw new AgentOutputException("TOOL_TIMEOUT", quotes.OutputJson);
        }

        using var document = JsonDocument.Parse(quotes.OutputJson);
        var items = document.RootElement.TryGetProperty("quotations", out var array)
            ? array.EnumerateArray().ToList()
            : [];
        var validIds = items.Select(x => x.GetProperty("id").GetGuid()).ToHashSet();

        var enrichments = new List<object>();
        foreach (var item in items)
        {
            var technicianId = item.GetProperty("technicianId").GetGuid();
            var args = JsonSerializer.Serialize(new { technicianId, requestId = input.RequestId }, AgentJson.Options);
            var reputation = await tools.CallAsync(context, "GetTechnicianReputation", args, cancellationToken);
            var jobs = await tools.CallAsync(context, "GetCompletedJobCount", args, cancellationToken);
            var distance = await tools.CallAsync(context, "GetDistanceBand", args, cancellationToken);
            used.AddRange(["GetTechnicianReputation", "GetCompletedJobCount", "GetDistanceBand"]);
            enrichments.Add(new { quotation = item, reputation = reputation.OutputJson, jobs = jobs.OutputJson, distance = distance.OutputJson });
        }

        await tools.CallAsync(context, "GetCustomerPreferences", JsonSerializer.Serialize(new { requestId = input.RequestId }, AgentJson.Options), cancellationToken);
        used.Add("GetCustomerPreferences");

        var payload = new
        {
            requestId = input.RequestId,
            validQuotationIds = validIds,
            enrichments,
            instruction = "Compare only these quotations. Never invent an id or a price. Do not book anyone."
        };

        var raw = await model.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = nameof(QuotationRecommendationAgent),
            Objective = "Compare real quotations without selecting one.",
            StructuredInputJson = JsonSerializer.Serialize(payload, AgentJson.Options),
            AllowedTools = tools.AllowedTools(Role),
            MaxOutputChars = maxOutputChars
        }, cancellationToken);

        var output = AgentOutputValidator.ValidateRecommendation(
            AgentSafety.Deserialize<RecommendationOutput>(raw, maxOutputChars),
            validIds);
        return (output, used.Distinct().ToList(), validIds);
    }
}
