using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Npgsql;

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
        raw = raw.Trim().Trim('"', '\'');
        var uri = Regex.Match(raw, @"postgres(?:ql)?://[^\s;]+", RegexOptions.IgnoreCase);
        if (uri.Success)
        {
            return FromBuilder(FromUri(uri.Value.TrimEnd('"', '\'')), raw);
        }

        if (raw.IndexOf('=') < 0)
        {
            if (LooksLikeHost(raw))
            {
                return FromBuilder(new NpgsqlConnectionStringBuilder { Host = raw }, raw);
            }

            throw new InvalidOperationException(
                "ConnectionStrings__Default must be the Render Internal Database URL (postgresql://...) or Host=...;Username=...;Password=...;Database=.... Do not paste the word inhost.");
        }

        raw = Regex.Replace(raw, @"\b(inhost|internalhost|internal host)\s*=", "Host=", RegexOptions.IgnoreCase);
        raw = Regex.Replace(raw, @"\b(initial catalog)\s*=", "Database=", RegexOptions.IgnoreCase);
        raw = Regex.Replace(raw, @"\b(user id|uid)\s*=", "Username=", RegexOptions.IgnoreCase);

        var builder = new NpgsqlConnectionStringBuilder();
        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim().Trim('"', '\'');
            if (key.Equals("inhost", StringComparison.OrdinalIgnoreCase)
                || key.Equals("internalhost", StringComparison.OrdinalIgnoreCase)
                || key.Equals("internal hostname", StringComparison.OrdinalIgnoreCase))
            {
                key = "Host";
            }

            try
            {
                builder[key] = value;
            }
            catch (ArgumentException)
            {
                // Ignore pasted labels Npgsql does not understand, such as "inhost".
            }
        }

        return FromBuilder(builder, raw);
    }

    private static NpgsqlConnectionStringBuilder FromUri(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
        };

        if (uri.Query.Contains("sslmode=disable", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = SslMode.Disable;
        }

        return builder;
    }

    private static string FromBuilder(NpgsqlConnectionStringBuilder builder, string original)
    {
        if (string.IsNullOrWhiteSpace(builder.Host) || builder.Host.Equals("inhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Postgres Host is missing. On Render, paste Internal Database URL into ConnectionStrings__Default (starts with postgresql://).");
        }

        if (NeedsSsl(builder.Host) || NeedsSsl(original))
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }

    private static bool LooksLikeHost(string value) =>
        value.Contains("dpg-", StringComparison.OrdinalIgnoreCase)
        || value.Contains("render.com", StringComparison.OrdinalIgnoreCase)
        || value.Contains('.');

    private static bool NeedsSsl(string value) =>
        value.Contains("render.com", StringComparison.OrdinalIgnoreCase)
        || value.Contains("dpg-", StringComparison.OrdinalIgnoreCase);
}
