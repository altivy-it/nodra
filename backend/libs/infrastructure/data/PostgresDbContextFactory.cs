using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nodra.Backend.Infra.Data.Postgres;

/// <summary>
/// Cria o contexto PostgreSQL durante operações de design-time do EF Core.
/// </summary>
public sealed class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    /// <summary>
    /// Cria um contexto usando a connection string injetada no processo de design-time.
    /// </summary>
    /// <param name="args">Argumentos opcionais da ferramenta EF Core.</param>
    /// <returns>Contexto configurado para PostgreSQL.</returns>
    /// <exception cref="InvalidOperationException">Quando a connection string não foi definida.</exception>
    public PostgresDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ALTIVY_DESIGNTIME_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Defina ALTIVY_DESIGNTIME_CONNECTION_STRING no processo para operações EF Core de design-time.");
        }

        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                _ = npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "altivy");
                _ = npgsqlOptions.CommandTimeout(180);
            })
            .Options;

        return new PostgresDbContext(options);
    }
}
