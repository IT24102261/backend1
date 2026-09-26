using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FixFlow.Infrastructure.Data;

public class FixFlowDbContextFactory : IDesignTimeDbContextFactory<FixFlowDbContext>
{
    public FixFlowDbContext CreateDbContext(string[] args)
    {
        var apiPath = ResolveApiPath();
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? ReadConnectionString(Path.Combine(apiPath, "appsettings.Local.json"))
            ?? ReadConnectionString(Path.Combine(apiPath, "appsettings.Development.json"))
            ?? ReadConnectionString(Path.Combine(apiPath, "appsettings.json"))
            ?? "Host=localhost;Port=5432;Database=FixFlow;Username=postgres";

        connectionString = PostgresConnection.Normalize(connectionString);

        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FixFlowDbContext(options);
    }

    private static string ResolveApiPath()
    {
        var current = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(current, "backend", "FixFlow.Api"),
            Path.Combine(current, "..", "FixFlow.Api"),
            Path.Combine(current, "FixFlow.Api")
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists)
            ?? current;
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.TryGetProperty("ConnectionStrings", out var strings)
            && strings.TryGetProperty("Default", out var value))
        {
            return value.GetString();
        }

        return null;
    }
}
