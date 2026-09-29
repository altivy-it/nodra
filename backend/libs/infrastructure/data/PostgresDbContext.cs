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

    /// <summary>
    /// Obtém a projeção reconstruível dos metadados das notas.
    /// </summary>
    public DbSet<NoteProjectionRecord> NoteProjections => Set<NoteProjectionRecord>();

    /// <summary>
    /// Obtém os jobs duráveis de indexação de notas.
    /// </summary>
    public DbSet<NoteIndexJobRecord> NoteIndexJobs => Set<NoteIndexJobRecord>();

    public DbSet<UserTrashRetentionSettingRecord> UserTrashRetentionSettings => Set<UserTrashRetentionSettingRecord>();

    public DbSet<TrashAuditRecord> TrashAuditEvents => Set<TrashAuditRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("altivy");
        modelBuilder.ApplyConfiguration(new VaultRecordConfiguration());
        modelBuilder.ApplyConfiguration(new VaultProvisioningOperationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new VaultResidueOccurrenceRecordConfiguration());
        modelBuilder.ApplyConfiguration(new NoteProjectionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new NoteIndexJobRecordConfiguration());
        modelBuilder.ApplyConfiguration(new UserTrashRetentionSettingRecordConfiguration());
        modelBuilder.ApplyConfiguration(new TrashAuditRecordConfiguration());
    }
}
