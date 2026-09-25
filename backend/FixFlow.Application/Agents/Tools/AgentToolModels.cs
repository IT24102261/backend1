using System.Text.Json;
using FixFlow.Domain.Enums;

namespace FixFlow.Application.Agents.Tools;

public sealed class AgentToolContext
{
    public required AgentRole Agent { get; init; }
    public Guid? WorkflowId { get; init; }
    public Guid? RequestId { get; init; }
    public Guid? ActorId { get; init; }
}

public sealed class AgentToolResult
{
    public bool Ok { get; init; } = true;
    public string OutputJson { get; init; } = "{}";
    public string? ErrorCode { get; init; }
    public string? ValidationJson { get; init; }

    public static AgentToolResult Success(object payload, object? validation = null) => new()
    {
        Ok = true,
        OutputJson = JsonSerializer.Serialize(payload, ToolJson.Options),
        ValidationJson = validation is null ? null : JsonSerializer.Serialize(validation, ToolJson.Options)
    };

    public static AgentToolResult Fail(string code, string message) => new()
    {
        Ok = false,
        ErrorCode = code,
        OutputJson = JsonSerializer.Serialize(new { error = message, code }, ToolJson.Options),
        ValidationJson = JsonSerializer.Serialize(new { valid = false, code, message }, ToolJson.Options)
    };
}

public static class ToolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}

public interface IAgentTool
{
    string Name { get; }
    string Purpose { get; }
    AgentRole AuthorizedAgent { get; }
    string InputSchema { get; }
    string OutputSchema { get; }
    TimeSpan Timeout { get; }
    Task<AgentToolResult> ExecuteAsync(AgentToolContext context, JsonElement input, CancellationToken cancellationToken);
}

public interface IAgentToolExecutor
{
    IReadOnlyList<string> AllowedTools(AgentRole agent);
    Task<AgentToolResult> CallAsync(AgentToolContext context, string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
