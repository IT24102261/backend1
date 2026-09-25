using System.Text.Json;
using FixFlow.Application.Interfaces;

namespace FixFlow.Infrastructure.ExternalServices;

public class ToolCaller : IToolCaller
{
    public Task<string> CallAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        var result = new
        {
            tool = toolName,
            arguments = argumentsJson,
            status = "ok"
        };

        return Task.FromResult(JsonSerializer.Serialize(result));
    }
}
