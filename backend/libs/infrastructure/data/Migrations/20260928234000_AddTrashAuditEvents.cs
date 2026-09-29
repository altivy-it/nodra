using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

/// <summary>
/// Cria a trilha append-only das operações de lixeira.
/// </summary>
public partial class AddTrashAuditEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "trash_audit_events",
            schema: "altivy",
            columns: table => new
            {
                audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                vault_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reauthenticated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_trash_audit_events", x => x.audit_id));

        migrationBuilder.CreateIndex(
            name: "IX_trash_audit_events_subject_occurred_at_utc",
            schema: "altivy",
            table: "trash_audit_events",
            columns: new[] { "subject", "occurred_at_utc" });
        migrationBuilder.CreateIndex(
            name: "IX_trash_audit_events_vault_id_occurred_at_utc",
            schema: "altivy",
            table: "trash_audit_events",
            columns: new[] { "vault_id", "occurred_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "trash_audit_events", schema: "altivy");
}
