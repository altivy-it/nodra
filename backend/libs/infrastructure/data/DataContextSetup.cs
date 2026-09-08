using Altivy.Backend.Application.Notes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altivy.Backend.Infra.Data.Postgres;

/// <summary>
/// Configura o acesso PostgreSQL da infraestrutura de dados.
/// </summary>
public static class DataContextSetup
{
    /// <summary>
    /// Registra o contexto PostgreSQL com pool e convenções do schema Altivy.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="connectionString">Connection string do PostgreSQL.</param>
    /// <returns>A coleção de serviços para composição adicional.</returns>
    public static IServiceCollection AddPostgresData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextPool<PostgresDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "altivy"));
        });

        services.AddScoped<INoteProjectionStore, PostgresNoteProjectionStore>();
        services.AddScoped<INoteIndexJobStore, PostgresNoteIndexJobStore>();
        services.AddScoped<PostgresVaultReader>();
        return services;
    }
}
