using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

/// <inheritdoc />
[DbContext(typeof(PostgresDbContext))]
[Migration("20260908170000_AddNoteIndexJobs")]
public partial class AddNoteIndexJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "note_index_jobs",
            schema: "altivy",
            columns: table => new
            {
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
                vault_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                state = table.Column<int>(type: "integer", nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_error = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_note_index_jobs", x => x.job_id));

        migrationBuilder.CreateIndex(
            name: "IX_note_index_jobs_idempotency_key",
            schema: "altivy",
            table: "note_index_jobs",
            column: "idempotency_key",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_note_index_jobs_state_next_attempt_at",
            schema: "altivy",
            table: "note_index_jobs",
            columns: new[] { "state", "next_attempt_at" });
        migrationBuilder.CreateIndex(
            name: "IX_note_index_jobs_state_lease_expires_at",
            schema: "altivy",
            table: "note_index_jobs",
            columns: new[] { "state", "lease_expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "note_index_jobs", schema: "altivy");
}
