using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

/// <inheritdoc />
[DbContext(typeof(PostgresDbContext))]
[Migration("20260906210000_AddVaultResidueRetentionWarning")]
public partial class AddVaultResidueRetentionWarning : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "alert_action_required", schema: "altivy", table: "vault_residue_occurrences", type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "investigate-vault-residue");
        migrationBuilder.AddColumn<DateTimeOffset>(name: "retention_warning_scheduled_at", schema: "altivy", table: "vault_residue_occurrences", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_vault_residue_occurrences_retention_warning_scheduled_at_last_detected_at", schema: "altivy", table: "vault_residue_occurrences", columns: new[] { "retention_warning_scheduled_at", "last_detected_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_vault_residue_occurrences_retention_warning_scheduled_at_last_detected_at", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "alert_action_required", schema: "altivy", table: "vault_residue_occurrences");
        migrationBuilder.DropColumn(name: "retention_warning_scheduled_at", schema: "altivy", table: "vault_residue_occurrences");
    }
}