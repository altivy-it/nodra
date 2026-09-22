using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class TemporalPartitionMigrationTests
{
    [Fact]
    public async Task MigrationsCreateMonthlyQueuePartitionsThrough2036()
    {
        await using var context = CreateMigratorContext();
        await context.Database.MigrateAsync();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        var noteJobPartitionCount = await GetMonthlyPartitionCountAsync(context, "note_index_jobs");
        var provisioningPartitionCount = await GetMonthlyPartitionCountAsync(
            context,
            "vault_provisioning_operations");
        var schedulingObjects = await context.Database
            .SqlQueryRaw<string>("""
                SELECT object_name AS "Value"
                FROM unnest(ARRAY[
                    to_regclass('altivy.note_index_jobs_default')::text,
                    to_regclass('altivy.vault_provisioning_operations_default')::text,
                    to_regclass('altivy.note_index_jobs_2036_12')::text,
                    to_regclass('altivy.vault_provisioning_operations_2036_12')::text,
                    to_regprocedure('altivy.altivy_ensure_monthly_queue_partitions(date)')::text
                ]) AS object_name
                """)
            .ToListAsync();

        Assert.Empty(pendingMigrations);
        Assert.Contains("20260908170000_AddNoteIndexJobs", appliedMigrations);
        Assert.Contains("202609220001_PartitionDurableQueues", appliedMigrations);
        Assert.Equal(132, noteJobPartitionCount);
        Assert.Equal(132, provisioningPartitionCount);
        Assert.DoesNotContain(schedulingObjects, string.IsNullOrWhiteSpace);
        Assert.Equal(5, schedulingObjects.Count);
    }

    [Fact]
    public async Task QueueRowsRouteToMonthlyAndDefaultPartitions()
    {
        await using var context = CreateMigratorContext();
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var monthlyJobId = Guid.NewGuid();
        var defaultJobId = Guid.NewGuid();
        var monthlyOperationId = Guid.NewGuid();
        var defaultOperationId = Guid.NewGuid();

        await InsertNoteJobAsync(context, monthlyJobId, new DateTimeOffset(2036, 12, 1, 0, 0, 0, TimeSpan.Zero));
        await InsertNoteJobAsync(context, defaultJobId, new DateTimeOffset(2041, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await InsertProvisioningOperationAsync(
            context,
            monthlyOperationId,
            new DateTimeOffset(2036, 12, 1, 0, 0, 0, TimeSpan.Zero));
        await InsertProvisioningOperationAsync(
            context,
            defaultOperationId,
            new DateTimeOffset(2041, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("note_index_jobs_2036_12", await GetNoteJobPartitionAsync(context, monthlyJobId));
        Assert.Equal("note_index_jobs_default", await GetNoteJobPartitionAsync(context, defaultJobId));
        Assert.Equal(
            "vault_provisioning_operations_2036_12",
            await GetProvisioningPartitionAsync(context, monthlyOperationId));
        Assert.Equal(
            "vault_provisioning_operations_default",
            await GetProvisioningPartitionAsync(context, defaultOperationId));
    }

    private static PostgresDbContext CreateMigratorContext()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Forneça ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING para o teste PostgreSQL.");
        }

        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "altivy"))
            .Options;
        return new PostgresDbContext(options);
    }

    private static async Task<int> GetMonthlyPartitionCountAsync(
        PostgresDbContext context,
        string parentTable)
    {
        var counts = await context.Database
            .SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value"
                FROM pg_inherits inheritance
                JOIN pg_class parent ON parent.oid = inheritance.inhparent
                JOIN pg_namespace namespace ON namespace.oid = parent.relnamespace
                JOIN pg_class child ON child.oid = inheritance.inhrelid
                WHERE namespace.nspname = 'altivy'
                  AND parent.relname = {0}
                  AND child.relname !~ '_default$'
                """, parentTable)
            .ToListAsync();

        return Assert.Single(counts);
    }

    private static Task<int> InsertNoteJobAsync(
        PostgresDbContext context,
        Guid jobId,
        DateTimeOffset createdAtUtc) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO altivy.note_index_jobs (
                job_id, created_at_utc, vault_id, idempotency_key, state, attempt_count)
            VALUES (
                {jobId}, {createdAtUtc}, {"partition-test"}, {jobId.ToString("N")}, {0}, {0})
            """);

    private static Task<int> InsertProvisioningOperationAsync(
        PostgresDbContext context,
        Guid operationId,
        DateTimeOffset createdAtUtc) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO altivy.vault_provisioning_operations (
                operation_id, created_at_utc, vault_id, name, subject,
                idempotency_key, state, attempt_count)
            VALUES (
                {operationId}, {createdAtUtc}, {"partition-test"}, {"Partition test"},
                {"partition-test"}, {operationId.ToString("N")}, {0}, {0})
            """);

    private static async Task<string> GetNoteJobPartitionAsync(
        PostgresDbContext context,
        Guid id)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT relation.relname
            FROM altivy.note_index_jobs parent
            JOIN pg_class relation ON relation.oid = parent.tableoid
            WHERE parent.job_id = @id
            """;
        AddIdParameter(command, id);

        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }

    private static async Task<string> GetProvisioningPartitionAsync(
        PostgresDbContext context,
        Guid id)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT relation.relname
            FROM altivy.vault_provisioning_operations parent
            JOIN pg_class relation ON relation.oid = parent.tableoid
            WHERE parent.operation_id = @id
            """;
        AddIdParameter(command, id);

        return Assert.IsType<string>(await command.ExecuteScalarAsync());
    }

    private static void AddIdParameter(System.Data.Common.DbCommand command, Guid id)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
    }
}
