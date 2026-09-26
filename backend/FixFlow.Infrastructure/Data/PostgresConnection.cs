using Microsoft.Extensions.Configuration;

namespace FixFlow.Infrastructure.Data;

public static class PostgresConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("Default")
            ?? configuration["DATABASE_URL"]
            ?? configuration["CONNECTION_STRING"]
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured. Set ConnectionStrings__Default or DATABASE_URL.");

        return Normalize(raw);
    }

    public static string Normalize(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
            var port = uri.IsDefaultPort ? 5432 : uri.Port;
            raw = $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password}";
        }

        if (NeedsSsl(raw)
            && raw.IndexOf("SSL Mode", StringComparison.OrdinalIgnoreCase) < 0
            && raw.IndexOf("Ssl Mode", StringComparison.OrdinalIgnoreCase) < 0)
        {
            raw = raw.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return raw;
    }

    private static bool NeedsSsl(string connectionString) =>
        connectionString.Contains("render.com", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("dpg-", StringComparison.OrdinalIgnoreCase);
}
