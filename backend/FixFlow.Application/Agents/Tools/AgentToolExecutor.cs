using System.Text.Json;
using FixFlow.Application.Agents;
using FixFlow.Application.Agents.Safety;
using FixFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixFlow.Application.Agents.Tools;

public sealed class AgentToolExecutor(
    IEnumerable<IAgentTool> tools,
    IOptions<AiOptions> options,
    ILogger<AgentToolExecutor> logger) : IAgentToolExecutor
{
    public IReadOnlyList<string> AllowedTools(AgentRole agent) =>
        tools.Where(x => x.AuthorizedAgent == agent).Select(x => x.Name).Distinct().ToList();

    public async Task<AgentToolResult> CallAsync(
        AgentToolContext context,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (AgentSafety.LooksLikeInjection(argumentsJson) && AgentSafety.IsPrimarilyInjection(argumentsJson))
        {
            return AgentToolResult.Fail("PROMPT_INJECTION", "User content cannot be used as a tool instruction.");
        }

        var tool = tools.FirstOrDefault(x =>
            x.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase) && x.AuthorizedAgent == context.Agent);
        if (tool is null)
        {
            return AgentToolResult.Fail("TOOL_NOT_ALLOWED", $"Agent {context.Agent} cannot call {toolName}.");
        }

        JsonElement input;
        try
        {
            input = string.IsNullOrWhiteSpace(argumentsJson)
                ? JsonDocument.Parse("{}").RootElement.Clone()
                : JsonDocument.Parse(argumentsJson).RootElement.Clone();
        }
        catch (JsonException)
        {
            return AgentToolResult.Fail("INVALID_TOOL_INPUT", "Tool arguments must be JSON.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(tool.Timeout == TimeSpan.Zero
            ? TimeSpan.FromSeconds(Math.Max(1, options.Value.ToolTimeoutSeconds))
            : tool.Timeout);

        try
        {
            var result = await tool.ExecuteAsync(context, input, timeout.Token);
            logger.LogInformation("Tool {Tool} for {Agent} completed with {Status}", tool.Name, context.Agent, result.Ok);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Tool {Tool} timed out", tool.Name);
            return AgentToolResult.Fail("TOOL_TIMEOUT", $"{tool.Name} exceeded its timeout.");
        }
    }
}
