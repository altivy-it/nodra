using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

/// <inheritdoc />
[DbContext(typeof(PostgresDbContext))]
[Migration("20260906190000_CreateVaultResidueOccurrences")]
public partial class CreateVaultResidueOccurrences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "vault_residue_occurrences",
            schema: "altivy",
            columns: table => new
            {
                occurrence_id = table.Column<Guid>(type: "uuid", nullable: false),
                vault_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                first_detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                alert_pending = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_vault_residue_occurrences", x => x.occurrence_id));

        migrationBuilder.CreateIndex(
            name: "IX_vault_residue_occurrences_alert_pending_last_detected_at",
            schema: "altivy",
            table: "vault_residue_occurrences",
            columns: new[] { "alert_pending", "last_detected_at" });

        migrationBuilder.CreateIndex(
            name: "IX_vault_residue_occurrences_vault_id_type",
            schema: "altivy",
            table: "vault_residue_occurrences",
            columns: new[] { "vault_id", "type" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "vault_residue_occurrences", schema: "altivy");
}