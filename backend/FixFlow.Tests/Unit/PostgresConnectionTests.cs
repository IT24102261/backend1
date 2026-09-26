using FixFlow.Infrastructure.Data;
using Npgsql;

namespace FixFlow.Tests.Unit;

public class PostgresConnectionTests
{
    [Fact]
    public void Normalize_AcceptsRenderUri()
    {
        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize(
            "postgresql://fixflow_user:secret@dpg-abc123-a/fixflow"));

        Assert.Equal("dpg-abc123-a", parsed.Host);
        Assert.Equal("fixflow_user", parsed.Username);
        Assert.Equal("fixflow", parsed.Database);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Normalize_MapsInhostToHost()
    {
        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize(
            "inhost=dpg-abc123-a;Port=5432;Database=fixflow;Username=fixflow_user;Password=secret"));

        Assert.Equal("dpg-abc123-a", parsed.Host);
    }

    [Fact]
    public void Normalize_AcceptsBareRenderHostname()
    {
        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("dpg-abc123-a"));

        Assert.Equal("dpg-abc123-a", parsed.Host);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Normalize_LeavesLocalhostWithoutSsl()
    {
        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize(
            "Host=localhost;Port=5432;Database=FixFlow;Username=postgres;Password=local"));

        Assert.Equal("localhost", parsed.Host);
        Assert.NotEqual(SslMode.Require, parsed.SslMode);
    }
}
