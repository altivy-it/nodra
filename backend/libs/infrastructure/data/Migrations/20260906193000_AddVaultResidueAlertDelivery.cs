using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

/// <inheritdoc />
[DbContext(typeof(PostgresDbContext))]
[Migration("20260906193000_AddVaultResidueAlertDelivery")]
public partial class AddVaultResidueAlertDelivery : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "alert_attempt_count", schema: "altivy", table: "vault_residue_occurrences", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "alert_last_error", schema: "altivy", table: "vault_residue_occurrences", type: "character varying(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "alert_lease_expires_at", schema: "altivy", table: "vault_residue_occurrences", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "alert_lease_id", schema: "altivy", table: "vault_residue_occurrences", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "next_alert_attempt_at", schema: "altivy", table: "vault_residue_occurrences", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql(MigrationSqlScriptLoader.Load(
            "20260906193000_AddVaultResidueAlertDelivery/Up/01-BackfillNextAlertAttempt.sql"));

        migrationBuilder.DropIndex(name: "IX_vault_residue_occurrences_alert_pending_last_detected_at", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.CreateIndex(name: "IX_vault_residue_occurrences_alert_pending_next_alert_attempt_at", schema: "altivy", table: "vault_residue_occurrences", columns: new[] { "alert_pending", "next_alert_attempt_at" });
        migrationBuilder.CreateIndex(name: "IX_vault_residue_occurrences_alert_pending_alert_lease_expires_at", schema: "altivy", table: "vault_residue_occurrences", columns: new[] { "alert_pending", "alert_lease_expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_vault_residue_occurrences_alert_pending_next_alert_attempt_at", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropIndex(name: "IX_vault_residue_occurrences_alert_pending_alert_lease_expires_at", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.CreateIndex(name: "IX_vault_residue_occurrences_alert_pending_last_detected_at", schema: "altivy", table: "vault_residue_occurrences", columns: new[] { "alert_pending", "last_detected_at" });
        migrationBuilder.DropColumn(name: "alert_attempt_count", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "alert_last_error", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "alert_lease_expires_at", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "alert_lease_id", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "next_alert_attempt_at", schema: "altivy", table: "vault_residue_occurrences");
    }
}
