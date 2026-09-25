using System.Text.Json;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Agents;

public class TechnicianMatchingAgent(IAiModelClient model, IAgentToolExecutor tools)
{
    public const AgentRole Role = AgentRole.TechnicianMatching;

    public async Task<(MatchingOutput Output, IReadOnlyList<string> ToolsUsed)> RunAsync(
        MatchingInput input,
        AgentToolContext context,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        var used = new List<string>();
        if (input.CategoryId is null)
        {
            return (new MatchingOutput(), used);
        }

        var catalog = await tools.CallAsync(
            context,
            "GetApprovedTechniciansByCategory",
            JsonSerializer.Serialize(new { categoryId = input.CategoryId }, AgentJson.Options),
            cancellationToken);
        used.Add("GetApprovedTechniciansByCategory");
        if (catalog.ErrorCode == "TOOL_TIMEOUT")
        {
            throw new AgentOutputException("TOOL_TIMEOUT", catalog.OutputJson);
        }

        using var document = JsonDocument.Parse(catalog.OutputJson);
        var candidates = document.RootElement.TryGetProperty("technicians", out var array)
            ? array.EnumerateArray().ToList()
            : [];

        var eligible = new List<EligibleTechnician>();
        var excluded = new List<ExcludedTechnician>();

        foreach (var candidate in candidates)
        {
            var technicianId = candidate.GetProperty("technicianId").GetGuid();
            var displayName = candidate.TryGetProperty("displayName", out var name) ? name.GetString() ?? string.Empty : string.Empty;
            var args = JsonSerializer.Serialize(new { technicianId, requestId = input.RequestId, input.PreferredStart, input.PreferredEnd }, AgentJson.Options);

            var active = await tools.CallAsync(context, "CheckTechnicianActive", args, cancellationToken);
            var area = await tools.CallAsync(context, "CheckServiceArea", args, cancellationToken);
            var availability = await tools.CallAsync(context, "CheckAvailability", args, cancellationToken);
            var capacity = await tools.CallAsync(context, "CheckCapacity", args, cancellationToken);
            await tools.CallAsync(context, "GetSkillTags", args, cancellationToken);
            used.AddRange(["CheckTechnicianActive", "CheckServiceArea", "CheckAvailability", "CheckCapacity", "GetSkillTags"]);

            if (ReadBool(active.OutputJson, "active") != true)
            {
                excluded.Add(new ExcludedTechnician { TechnicianId = technicianId, Reason = "Account inactive or suspended." });
                continue;
            }

            if (ReadBool(area.OutputJson, "matches") != true)
            {
                excluded.Add(new ExcludedTechnician { TechnicianId = technicianId, Reason = "Service area does not match." });
                continue;
            }

            if (ReadBool(availability.OutputJson, "available") != true)
            {
                excluded.Add(new ExcludedTechnician { TechnicianId = technicianId, Reason = "Technician is unavailable in the preferred window." });
                continue;
            }

            if (ReadBool(capacity.OutputJson, "withinCapacity") != true)
            {
                excluded.Add(new ExcludedTechnician { TechnicianId = technicianId, Reason = "Technician is at capacity." });
                continue;
            }

            if (candidate.TryGetProperty("approved", out var approved) && !approved.GetBoolean())
            {
                excluded.Add(new ExcludedTechnician { TechnicianId = technicianId, Reason = "Registration is not verification." });
                continue;
            }

            eligible.Add(new EligibleTechnician
            {
                TechnicianId = technicianId,
                DisplayName = displayName,
                ServiceArea = candidate.TryGetProperty("serviceArea", out var areaEl) ? areaEl.GetString() : null
            });
        }

        var payload = new
        {
            requestId = input.RequestId,
            eligible,
            excluded,
            instruction = "Do not add technicians who failed the tool checks. Registration is not verification."
        };

        var raw = await model.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = nameof(TechnicianMatchingAgent),
            Objective = "Return only eligible technicians after tool checks.",
            StructuredInputJson = JsonSerializer.Serialize(payload, AgentJson.Options),
            AllowedTools = tools.AllowedTools(Role),
            MaxOutputChars = maxOutputChars
        }, cancellationToken);

        var modelOutput = AgentSafety.Deserialize<MatchingOutput>(raw, maxOutputChars);
        var allowedIds = eligible.Select(x => x.TechnicianId).ToHashSet();
        modelOutput.EligibleTechnicians = (modelOutput.EligibleTechnicians ?? [])
            .Where(x => allowedIds.Contains(x.TechnicianId))
            .ToList();
        if (modelOutput.EligibleTechnicians.Count == 0)
        {
            modelOutput.EligibleTechnicians = eligible;
        }

        modelOutput.ExcludedTechnicians = excluded;
        return (modelOutput, used.Distinct().ToList());
    }

    private static bool? ReadBool(string json, string name)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
    }
}
