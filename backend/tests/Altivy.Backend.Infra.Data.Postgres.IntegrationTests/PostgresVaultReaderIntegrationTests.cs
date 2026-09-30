using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Domain.Vaults;
using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Altivy.Backend.Infra.Data.Postgres.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("Shared PostgreSQL schema")]
public sealed class PostgresVaultReaderIntegrationTests : IAsyncLifetime
{
    private readonly string migratorConnectionString;
    private readonly string runtimeConnectionString;

    private readonly string testVaultId = $"integration-{Guid.NewGuid():N}";
    private readonly string pageVaultPrefix = $"zz-integration-{Guid.NewGuid():N}";
    private readonly string operationSubject = $"integration-subject-{Guid.NewGuid():N}";

    public PostgresVaultReaderIntegrationTests()
    {
        migratorConnectionString = GetConnectionString(
            "ALTIVY_INTEGRATION_MIGRATOR_CONNECTION_STRING");
        runtimeConnectionString = GetConnectionString(
            "ALTIVY_INTEGRATION_RUNTIME_CONNECTION_STRING");
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext(migratorConnectionString);
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync(
            "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA altivy TO altivy_runtime");
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext(migratorConnectionString);
        await context.VaultResidueOccurrences
            .Where(occurrence => occurrence.VaultId == testVaultId)
            .ExecuteDeleteAsync();
        await context.Vaults
            .Where(vault => vault.Id == testVaultId || vault.Id.StartsWith(pageVaultPrefix))
            .ExecuteDeleteAsync();
        await context.VaultProvisioningOperations
            .Where(operation => operation.Subject == operationSubject)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task PostgresVaultReaderReadsProjectedVaultWithRuntimeRole()
    {
        await using (var migratorContext = CreateContext(migratorConnectionString))
        {
            migratorContext.Vaults.Add(new VaultRecord
            {
                Id = testVaultId,
                Name = "Integração PostgreSQL"
            });
            await migratorContext.SaveChangesAsync();
        }

        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var reader = new PostgresVaultReader(runtimeContext);

        var vault = await reader.FindAsync(testVaultId, CancellationToken.None);

        Assert.NotNull(vault);
        Assert.Equal(testVaultId, vault.Id);
        Assert.Equal("Integração PostgreSQL", vault.Name);
    }

    [Fact]
    public async Task PostgresVaultReaderPaginatesCandidatesInStableIdOrder()
    {
        var expectedIds = new[]
        {
            $"{pageVaultPrefix}-a",
            $"{pageVaultPrefix}-b",
            $"{pageVaultPrefix}-c"
        };
        await using (var migratorContext = CreateContext(migratorConnectionString))
        {
            migratorContext.Vaults.AddRange(expectedIds.Select((id, index) => new VaultRecord
            {
                Id = id,
                Name = $"Vault {index}"
            }));
            await migratorContext.SaveChangesAsync();
        }

        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var reader = new PostgresVaultReader(runtimeContext);
        var observedIds = new List<string>();
        string? cursor = null;

        do
        {
            var page = await reader.ReadCandidatesAsync(cursor, 1, CancellationToken.None);
            observedIds.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(expectedIds, observedIds.Where(id => id.StartsWith(pageVaultPrefix, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task PostgresVaultReaderRejectsInvalidCursor()
    {
        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var reader = new PostgresVaultReader(runtimeContext);

        await Assert.ThrowsAsync<InvalidVaultCursorException>(() =>
            reader.ReadCandidatesAsync("not-a-cursor", 20, CancellationToken.None));
    }

    [Fact]
    public async Task PostgresVaultProvisioningStorePersistsAndFindsByIdempotencyKey()
    {
        var operation = VaultProvisioningOperation.Create(
            Guid.NewGuid(), testVaultId, "Vault de integração", operationSubject);
        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);

        await store.AddAsync(operation, "request-1", CancellationToken.None);
        var found = await store.FindByIdempotencyKeyAsync(
            operationSubject, "request-1", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(operation.OperationId, found.OperationId);
        Assert.Equal(testVaultId, found.VaultId);
        Assert.Equal(VaultLifecycleState.Pending, found.State);
    }

    [Fact]
    public async Task PostgresVaultProvisioningStoreClaimsAndCompletesOperationWithExclusiveLease()
    {
        var operation = VaultProvisioningOperation.Create(
            Guid.NewGuid(), testVaultId, "Vault de integração", operationSubject);
        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);
        await store.AddAsync(operation, "request-claim", CancellationToken.None);

        var workItem = await store.ClaimNextAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
        var duplicateClaim = await store.ClaimNextAsync(
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        Assert.NotNull(workItem);
        Assert.Null(duplicateClaim);
        workItem.Operation.TransitionTo(VaultLifecycleState.Ready);
        var completed = await store.CompleteAsync(workItem, null, null, CancellationToken.None);

        Assert.True(completed);
        var vault = await runtimeContext.Vaults.SingleAsync(record => record.Id == testVaultId);
        Assert.Equal(VaultLifecycleState.Ready, vault.State);
    }

    [Fact]
    public async Task PostgresVaultProvisioningStoreRegistersMissingDirectoryOccurrenceIdempotently()
    {
        await using (var migratorContext = CreateContext(migratorConnectionString))
        {
            migratorContext.Vaults.Add(new VaultRecord
            {
                Id = testVaultId,
                Name = "Vault de integração",
                State = VaultLifecycleState.Ready
            });
            await migratorContext.SaveChangesAsync();
        }

        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);

        var firstDetectedAt = DateTimeOffset.UtcNow;
        await store.RegisterMissingDirectoryAsync(testVaultId, firstDetectedAt, CancellationToken.None);
        var firstOccurrence = await runtimeContext.VaultResidueOccurrences.SingleAsync(
            occurrence => occurrence.VaultId == testVaultId);
        var secondDetectedAt = firstDetectedAt.AddMinutes(1);
        await store.RegisterMissingDirectoryAsync(testVaultId, secondDetectedAt, CancellationToken.None);

        var occurrence = await runtimeContext.VaultResidueOccurrences.SingleAsync(
            record => record.VaultId == testVaultId);
        var vault = await runtimeContext.Vaults.SingleAsync(record => record.Id == testVaultId);
        Assert.Equal(VaultLifecycleState.Degraded, vault.State);
        Assert.Equal(firstOccurrence.OccurrenceId, occurrence.OccurrenceId);
        Assert.Equal(firstDetectedAt, occurrence.FirstDetectedAt);
        Assert.Equal(secondDetectedAt, occurrence.LastDetectedAt);
        Assert.True(occurrence.AlertPending);
    }

    [Fact]
    public async Task PostgresVaultProvisioningStoreClaimsAndCompletesResidueAlertWithExclusiveLease()
    {
        await using (var migratorContext = CreateContext(migratorConnectionString))
        {
            migratorContext.Vaults.Add(new VaultRecord
            {
                Id = testVaultId,
                Name = "Vault de integração",
                State = VaultLifecycleState.Ready
            });
            await migratorContext.SaveChangesAsync();
        }

        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);
        var detectedAt = DateTimeOffset.UtcNow;
        await store.RegisterMissingDirectoryAsync(testVaultId, detectedAt, CancellationToken.None);

        var workItem = await store.ClaimNextAlertAsync(detectedAt, TimeSpan.FromMinutes(5), CancellationToken.None);
        var duplicateClaim = await store.ClaimNextAlertAsync(detectedAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.NotNull(workItem);
        Assert.Null(duplicateClaim);
        Assert.Equal("missing-directory/v1", workItem.Alert.Type);
        Assert.Equal(1, workItem.Alert.AttemptCount);
        Assert.True(await store.CompleteAlertAsync(workItem, null, null, CancellationToken.None));

        var occurrence = await runtimeContext.VaultResidueOccurrences
            .AsNoTracking()
            .SingleAsync(record => record.OccurrenceId == workItem.Alert.OccurrenceId);
        Assert.False(occurrence.AlertPending);
        Assert.Equal(1, occurrence.AlertAttemptCount);
        Assert.Null(occurrence.AlertLeaseId);
        Assert.Null(occurrence.AlertLeaseExpiresAt);
    }

    [Fact]
    public async Task PostgresVaultProvisioningStoreRegistersOrphanedDirectoryOccurrenceIdempotently()
    {
        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);
        var firstDetectedAt = DateTimeOffset.UnixEpoch.AddSeconds(1);

        await store.RegisterOrphanedDirectoryAsync(testVaultId, firstDetectedAt, CancellationToken.None);
        var firstOccurrence = await runtimeContext.VaultResidueOccurrences
            .AsNoTracking()
            .SingleAsync(occurrence => occurrence.VaultId == testVaultId);
        var secondDetectedAt = firstDetectedAt.AddMinutes(1);
        await store.RegisterOrphanedDirectoryAsync(testVaultId, secondDetectedAt, CancellationToken.None);

        var occurrence = await runtimeContext.VaultResidueOccurrences
            .AsNoTracking()
            .SingleAsync(record => record.VaultId == testVaultId);
        Assert.Equal("orphaned-directory/v1", occurrence.Type);
        Assert.Equal(firstOccurrence.OccurrenceId, occurrence.OccurrenceId);
        Assert.Equal(firstDetectedAt, occurrence.FirstDetectedAt);
        Assert.Equal(secondDetectedAt, occurrence.LastDetectedAt);
        Assert.True(occurrence.AlertPending);
        Assert.Equal(secondDetectedAt, occurrence.NextAlertAttemptAt);
    }

    [Fact]
    public async Task PostgresVaultProvisioningStoreSchedulesOneRetentionWarningAfterThirtyDays()
    {
        await using var runtimeContext = CreateContext(runtimeConnectionString);
        var store = new PostgresVaultProvisioningStore(runtimeContext);
        var detectedAt = DateTimeOffset.UnixEpoch;
        await store.RegisterOrphanedDirectoryAsync(testVaultId, detectedAt, CancellationToken.None);
        var initialAlert = await store.ClaimNextAlertAsync(detectedAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.NotNull(initialAlert);
        Assert.True(await store.CompleteAlertAsync(initialAlert, null, null, CancellationToken.None));

        var expirationAt = detectedAt.AddDays(30);
        var scheduledCount = await store.ScheduleRetentionWarningsAsync(expirationAt, CancellationToken.None);
        var retentionAlert = await store.ClaimNextAlertAsync(expirationAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Equal(1, scheduledCount);
        Assert.NotNull(retentionAlert);
        Assert.Equal("retention-expired", retentionAlert.Alert.ActionRequired);
        Assert.True(await store.CompleteAlertAsync(retentionAlert, null, null, CancellationToken.None));
        Assert.Equal(0, await store.ScheduleRetentionWarningsAsync(expirationAt, CancellationToken.None));
    }

    private static PostgresDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "altivy"))
            .Options;
        return new PostgresDbContext(options);
    }

    private static string GetConnectionString(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            var connectionString = new NpgsqlConnectionStringBuilder(value);
            if (connectionString.Port == 55432)
            {
                throw new InvalidOperationException(
                    "A suíte Integration deve usar o PostgreSQL principal em localhost:5432, não altivy-postgres-test.");
            }

            return value;
        }

        throw new InvalidOperationException(
            $"Inicie a plataforma central architecture-knowledge-base/platforms/identity e forneça {variableName} para o PostgreSQL principal.");
    }
}
