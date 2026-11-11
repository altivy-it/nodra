using Nodra.Backend.Infra.Data.Postgres.Migrations;
using Xunit;

namespace Nodra.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Unit")]
public sealed class MigrationSqlScriptLoaderTests
{
    [Fact]
    public void LoadReadsEmbeddedPostgreSqlAndRejectsMissingResource()
    {
        var script = MigrationSqlScriptLoader.Load(
            "20260906193000_AddVaultResidueAlertDelivery/Up/01-BackfillNextAlertAttempt.sql");

        Assert.Contains("next_alert_attempt_at = last_detected_at", script, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() =>
            MigrationSqlScriptLoader.Load("missing/Up/01-Missing.sql"));
    }
}
