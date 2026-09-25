using FixFlow.Application.Agents;
using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FixFlow.Tests.Support;

public sealed class FixFlowApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = connectionString;
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "fixflow-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:Issuer"] = "FixFlow",
                ["Jwt:Audience"] = "FixFlowClients",
                ["Jwt:Secret"] = "FixFlowLocalDevJwtSecretKey_ChangeBeforeProduction!",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Maps:Provider"] = "Disabled",
                ["Ai:Provider"] = "Deterministic",
                ["Ai:MaxRetries"] = "2",
                ["Storage:Root"] = StorageRoot
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMapService>();
            services.AddSingleton<IMapService, CoordinateOnlyMapService>();
            services.RemoveAll<IAgentOrchestrator>();
            services.AddScoped<AgentOrchestrator>();
            services.AddScoped<IAgentOrchestrator>(sp => new SafeTestOrchestrator(sp.GetRequiredService<AgentOrchestrator>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(StorageRoot))
        {
            try
            {
                Directory.Delete(StorageRoot, true);
            }
            catch
            {
                // Temp files are disposable.
            }
        }
    }
}
