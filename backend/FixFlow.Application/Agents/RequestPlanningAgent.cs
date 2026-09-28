using System.Text.Json;
using FixFlow.Application.Agents.Contracts;
using FixFlow.Application.Agents.Safety;
using FixFlow.Application.Agents.Tools;
using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Agents;

public class RequestPlanningAgent(
    IAiModelClient model,
    IAgentToolExecutor tools,
    IRepository<ServiceCategory> categories)
{
    public const AgentRole Role = AgentRole.RequestPlanning;

    public async Task<(PlanningOutput Output, IReadOnlyList<string> ToolsUsed)> RunAsync(
        PlanningInput input,
        AgentToolContext context,
        int maxOutputChars,
        CancellationToken cancellationToken)
    {
        if (AgentSafety.IsPrimarilyInjection(input.Description))
        {
            throw new AgentOutputException("PROMPT_INJECTION", "The request text was treated as an instruction and rejected.");
        }

        var used = new List<string>();
        await Call(tools, context, "GetServiceCategories", "{}", used, cancellationToken);
        await Call(tools, context, "GetRequest", JsonSerializer.Serialize(new { requestId = input.RequestId }, AgentJson.Options), used, cancellationToken);

        var catalog = await LoadCatalog(cancellationToken);
        var category = catalog.FirstOrDefault(x =>
            x.Name.Equals(input.OptionalCategory, StringComparison.OrdinalIgnoreCase)
            || (input.OptionalCategory?.Equals("Electrical", StringComparison.OrdinalIgnoreCase) == true && x.Name == "Electrician"));
        if (category is not null)
        {
            await Call(tools, context, "GetCategoryRequirements", JsonSerializer.Serialize(new { categoryId = category.Id }, AgentJson.Options), used, cancellationToken);
        }

        var payload = new
        {
            untrustedCustomerText = AgentSafety.SanitizeUserText(input.Description),
            optionalCategory = input.OptionalCategory,
            serviceArea = input.ServiceArea,
            preferredStart = input.PreferredStart,
            preferredEnd = input.PreferredEnd,
            images = input.Images,
            catalog = catalog.Select(x => x.Name),
            instruction = "Classify using catalog names only. Never invent a category. Never give repair instructions."
        };

        var raw = await model.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = nameof(RequestPlanningAgent),
            Objective = "Classify the service request and produce a structured plan.",
            StructuredInputJson = JsonSerializer.Serialize(payload, AgentJson.Options),
            AllowedTools = tools.AllowedTools(Role),
            MaxOutputChars = maxOutputChars
        }, cancellationToken);

        var output = AgentOutputValidator.ValidatePlanning(
            AgentSafety.Deserialize<PlanningOutput>(raw, maxOutputChars),
            catalog);

        if (AgentSafety.IsDangerousElectrical(input.Description))
        {
            output.ClarificationRequired = true;
            if (!output.ClarificationQuestions.Any(x => x.Contains("licensed electrician", StringComparison.OrdinalIgnoreCase)))
            {
                output.ClarificationQuestions.Insert(0, "This looks like electrical work. A licensed electrician must attend. Do not attempt live-wire repairs. Confirm access and that you want a professional visit only.");
            }
        }

        if (NeedsChangeCount(input.Description, output.Category, output.Quantity))
        {
            output.Quantity = null;
            output.ClarificationRequired = true;
            if (!output.ClarificationQuestions.Any(x => x.Contains("how many", StringComparison.OrdinalIgnoreCase)))
            {
                output.ClarificationQuestions.Add("How many need to be changed?");
            }

            if (!output.MissingInformation.Any(x => x.Contains("quantity", StringComparison.OrdinalIgnoreCase)))
            {
                output.MissingInformation.Add("quantity to change");
            }
        }

        return (output, used);
    }

    private static readonly string[] CountableFittings =
    [
        "switch", "plug", "socket", "outlet", "bulb", "light", "fan", "breaker", "tap", "faucet"
    ];

    private static bool NeedsChangeCount(string description, string category, int? quantity)
    {
        if (quantity is not null || HasExplicitCount(description))
        {
            return false;
        }

        var text = description.ToLowerInvariant();
        if (!CountableFittings.Any(text.Contains))
        {
            return false;
        }

        var electrical = category.Equals("Electrician", StringComparison.OrdinalIgnoreCase)
            || category.Equals("Electrical", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(category);
        var plumbing = category.Equals("Plumber", StringComparison.OrdinalIgnoreCase);
        if (!electrical && !plumbing)
        {
            return false;
        }

        return true;
    }

    private static bool HasExplicitCount(string description)
    {
        if (description.Any(char.IsDigit))
        {
            return true;
        }

        var value = description.ToLowerInvariant();
        string[] counts = ["one", "two", "three", "four", "five", "several", "multiple"];
        return counts.Any(count => value.Contains(count, StringComparison.Ordinal));
    }

    private Task<List<ServiceCategory>> LoadCatalog(CancellationToken cancellationToken) =>
        categories.Query().Where(x => x.IsActive).OrderBy(x => x.Name).ToMaterializedListAsync(cancellationToken);

    private static async Task Call(
        IAgentToolExecutor tools,
        AgentToolContext context,
        string name,
        string args,
        List<string> used,
        CancellationToken cancellationToken)
    {
        var result = await tools.CallAsync(context, name, args, cancellationToken);
        used.Add(name);
        if (result.ErrorCode == "TOOL_TIMEOUT")
        {
            throw new AgentOutputException("TOOL_TIMEOUT", result.OutputJson);
        }
    }
}
