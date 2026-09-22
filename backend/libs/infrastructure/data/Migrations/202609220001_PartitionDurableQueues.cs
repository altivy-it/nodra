using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altivy.Backend.Infra.Data.Postgres.Migrations;

[DbContext(typeof(PostgresDbContext))]
[Migration("202609220001_PartitionDurableQueues")]
public sealed class PartitionDurableQueues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(MigrationSqlScriptLoader.Load(
            "202609220001_PartitionDurableQueues/Up/01-PartitionDurableQueues.sql"));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Queue partitioning is forward-only to preserve durable idempotency keys.");
    }
}
