using Nodra.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nodra.Backend.Infra.Data.Postgres.Migrations;

/// <inheritdoc />
[DbContext(typeof(PostgresDbContext))]
[Migration("20260906183000_AddVaultProvisioningLease")]
public partial class AddVaultProvisioningLease : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "lease_expires_at",
            schema: "altivy",
            table: "vault_provisioning_operations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "lease_id",
            schema: "altivy",
            table: "vault_provisioning_operations",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_vault_provisioning_operations_state_lease_expires_at",
            schema: "altivy",
            table: "vault_provisioning_operations",
            columns: new[] { "state", "lease_expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_vault_provisioning_operations_state_lease_expires_at",
            schema: "altivy",
            table: "vault_provisioning_operations");

        migrationBuilder.DropColumn(
            name: "lease_expires_at",
            schema: "altivy",
            table: "vault_provisioning_operations");

        migrationBuilder.DropColumn(
            name: "lease_id",
            schema: "altivy",
            table: "vault_provisioning_operations");
    }
}
