using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nodra.Backend.Infra.Data.Postgres.Migrations;

/// <summary>
/// Registra o vault de sistema de exemplo na projeção de vaults.
/// </summary>
[DbContext(typeof(PostgresDbContext))]
[Migration("20261123120000_AddSampleVault")]
public partial class AddSampleVault : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            INSERT INTO altivy.vaults ("Id", "Name", state)
            VALUES ('nodra-exemplo', 'Vault de exemplo', 2)
            ON CONFLICT ("Id") DO NOTHING;
            """);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""DELETE FROM altivy.vaults WHERE "Id" = 'nodra-exemplo';""");
}
