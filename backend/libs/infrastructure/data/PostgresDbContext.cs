using Microsoft.EntityFrameworkCore;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Contexto EF Core da projeção PostgreSQL do Altivy.
/// </summary>
public sealed class PostgresDbContext(DbContextOptions<PostgresDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Obtém os vaults projetados para consulta.
    /// </summary>
    public DbSet<VaultRecord> Vaults => Set<VaultRecord>();

    /// <summary>
    /// Obtém as operações de provisioning persistidas.
    /// </summary>
    public DbSet<VaultProvisioningOperationRecord> VaultProvisioningOperations => Set<VaultProvisioningOperationRecord>();

    /// <summary>
    /// Obtém as ocorrências operacionais de resíduos de vault.
    /// </summary>
    public DbSet<VaultResidueOccurrenceRecord> VaultResidueOccurrences => Set<VaultResidueOccurrenceRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("altivy");
        modelBuilder.ApplyConfiguration(new VaultRecordConfiguration());
        modelBuilder.ApplyConfiguration(new VaultProvisioningOperationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new VaultResidueOccurrenceRecordConfiguration());
    }
}
