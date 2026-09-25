using System.Text.Json;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FixFlow.Tests.Support;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private PostgreSqlContainer? _container;
    private string? _localDatabase;
    private string? _adminConnectionString;

    public string ConnectionString { get; private set; } = string.Empty;

    public static async Task<PostgresTestDatabase> StartAsync()
    {
        var database = new PostgresTestDatabase();
        if (await database.TryStartContainerAsync())
        {
            return database;
        }

        await database.StartLocalAsync();
        return database;
    }

    private async Task<bool> TryStartContainerAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("fixflow_tests")
                .WithUsername("postgres")
                .WithPassword("test")
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            await _container.StartAsync(cts.Token);
            ConnectionString = _container.GetConnectionString();
            return true;
        }
        catch
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            return false;
        }
    }

    private async Task StartLocalAsync()
    {
        var template = ResolveLocalConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(template)
        {
            Database = "postgres"
        };
        _adminConnectionString = builder.ConnectionString;
        _localDatabase = $"fixflow_it_{Guid.NewGuid():N}"[..31];

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($@"CREATE DATABASE ""{_localDatabase}"";", admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = _localDatabase;
        ConnectionString = builder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        if (_adminConnectionString is null || _localDatabase is null)
        {
            return;
        }

        try
        {
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using var terminate = new NpgsqlCommand(
                $@"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_localDatabase}' AND pid <> pg_backend_pid();",
                admin);
            await terminate.ExecuteNonQueryAsync();
            await using var drop = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{_localDatabase}"";", admin);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup of a disposable local test database.
        }
    }

    private static string ResolveLocalConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("FIXFLOW_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        foreach (var path in LocalSettingsCandidates())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var section)
                && section.TryGetProperty("Default", out var value))
            {
                var connection = value.GetString();
                if (!string.IsNullOrWhiteSpace(connection))
                {
                    return connection;
                }
            }
        }

        return "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
    }

    private static IEnumerable<string> LocalSettingsCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FixFlow.Api", "appsettings.Local.json"));
        yield return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "backend", "FixFlow.Api", "appsettings.Local.json"));
        yield return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "FixFlow.Api", "appsettings.Local.json"));
    }
}
