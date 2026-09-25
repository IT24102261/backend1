using FixFlow.Application.Agents;
using FixFlow.Application.DTOs.Agents;
using FixFlow.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FixFlow.Tests.Unit;

public class AgentOrchestratorTests
{
    [Fact]
    public async Task DeterministicClient_ReturnsJsonWithoutSecrets()
    {
        var client = new Infrastructure.ExternalServices.DeterministicAiModelClient(Options.Create(new AiOptions()));
        var json = await client.CompleteJsonAsync(new AiCompletionRequest
        {
            AgentName = "Compatibility",
            Objective = "Triage leaking tap",
            StructuredInputJson = """{"prompt":"Triage leaking tap"}"""
        });

        Assert.Contains("ok", json);
        Assert.DoesNotContain("Bearer", json);
        Assert.DoesNotContain("password", json);
    }
}
