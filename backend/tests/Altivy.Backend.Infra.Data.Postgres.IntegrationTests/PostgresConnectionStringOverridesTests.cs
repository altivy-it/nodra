using Altivy.Backend.Infra.Data.Postgres;
using Npgsql;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Unit")]
public sealed class PostgresConnectionStringOverridesTests
{
    [Fact]
    public void WithHostChangesOnlyTheHost()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=keycloak;Username=altivy_runtime;Password=test-only;Search Path=altivy";

        var result = new NpgsqlConnectionStringBuilder(
            PostgresConnectionStringOverrides.WithHost(connectionString, "postgres"));

        Assert.Equal("postgres", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("keycloak", result.Database);
        Assert.Equal("altivy_runtime", result.Username);
        Assert.Equal("test-only", result.Password);
        Assert.Equal("altivy", result.SearchPath);
    }
}
