using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nodra.Backend.Infra.Data.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class CreateVaultProvisioningOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "state",
                schema: "altivy",
                table: "vaults",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "vault_provisioning_operations",
                schema: "altivy",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vault_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vault_provisioning_operations", x => x.operation_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vault_provisioning_operations_state_next_attempt_at",
                schema: "altivy",
                table: "vault_provisioning_operations",
                columns: new[] { "state", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_vault_provisioning_operations_subject_idempotency_key",
                schema: "altivy",
                table: "vault_provisioning_operations",
                columns: new[] { "subject", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vault_provisioning_operations_vault_id",
                schema: "altivy",
                table: "vault_provisioning_operations",
                column: "vault_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vault_provisioning_operations",
                schema: "altivy");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "altivy",
                table: "vaults");
        }
    }
}
