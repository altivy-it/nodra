using Nodra.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nodra.Backend.Infra.Data.Postgres.Migrations;

/// <summary>
/// Cria o armazenamento do parâmetro de retenção da lixeira por sujeito.
/// </summary>
[DbContext(typeof(PostgresDbContext))]
[Migration("20260928232500_AddUserTrashRetentionSettings")]
public partial class AddUserTrashRetentionSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_trash_retention_settings",
            schema: "altivy",
            columns: table => new
            {
                subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                retention_days = table.Column<int>(type: "integer", nullable: true),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_trash_retention_settings", x => x.subject);
                table.CheckConstraint(
                    "ck_user_trash_retention_settings_days",
                    "retention_days IS NULL OR (retention_days >= 1 AND retention_days <= 3650)");
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "user_trash_retention_settings", schema: "altivy");
}
