using System.Text.Json;
using FixFlow.Application.Interfaces;

namespace FixFlow.Infrastructure.ExternalServices;

public class LlmModelAdapter : ILlmModelAdapter
{
    public Task<string> CompleteJsonAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            intent = "triage_service_request",
            prompt,
            recommendedAction = "assign_technician"
        };

        return Task.FromResult(JsonSerializer.Serialize(payload));
    }
}
