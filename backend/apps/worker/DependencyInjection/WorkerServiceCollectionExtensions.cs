using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Application.Notes;
using Nodra.Backend.Infra.Data.Postgres;
using Nodra.Notes.Worker.Notifications;
using Nodra.Notes.Worker.Notes;
using Nodra.Notes.Worker.Observability;
using Nodra.Notes.Worker.Vaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nodra.Notes.Worker.DependencyInjection;

/// <summary>
/// Registra os serviços necessários ao processamento de provisioning de vaults.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    private static readonly string[] SmtpConfigurationKeys = ["Host", "Username", "Password", "From", "To"];

    /// <summary>
    /// Adiciona persistência, filesystem e o worker de operações de vault.
    /// </summary>
    /// <param name="services">Coleção de serviços do host.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <param name="environment">Ambiente de execução.</param>
    /// <returns>A coleção de serviços para composição.</returns>
    /// <exception cref="InvalidOperationException">Quando o PostgreSQL não é configurado fora de desenvolvimento.</exception>
    public static IServiceCollection AddVaultProvisioningWorker(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("AltivyPostgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:AltivyPostgres é obrigatória para o worker.");
        }

        var postgresHostOverride = configuration["Database:PostgresHostOverride"];
        if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(postgresHostOverride))
        {
            connectionString = PostgresConnectionStringOverrides.WithHost(connectionString, postgresHostOverride);
        }

        var vaultRoot = configuration["VaultStorage:Root"];
        if (string.IsNullOrWhiteSpace(vaultRoot))
        {
            vaultRoot = Path.Combine(environment.ContentRootPath, "vaults");
        }

        _ = services.AddPostgresData(connectionString);
        _ = services.AddWorkerOpenTelemetry(configuration);
        _ = services.AddScoped<IVaultProvisioningStore, PostgresVaultProvisioningStore>();
        _ = services.AddScoped<IVaultResidueAlertStore, PostgresVaultProvisioningStore>();
        _ = services.AddSingleton<TimeProvider>(TimeProvider.System);
        _ = services.AddSingleton<IVaultFileSystemProvisioner>(new FileSystemVaultProvisioner(vaultRoot));
        _ = services.AddSingleton<INoteStore>(new FileSystemNoteStore(vaultRoot));
        _ = services.AddScoped<NoteIndexJobProcessor>();
        var smtpSection = configuration.GetSection(AdministrativeSmtpOptions.SectionName);
        var smtpIsConfigured = SmtpConfigurationKeys.Any(key => !string.IsNullOrWhiteSpace(smtpSection[key]));
        if (environment.IsDevelopment() && !smtpIsConfigured)
        {
            _ = services.AddSingleton<IAdministrativeNotifier, DevelopmentAdministrativeNotifier>();
        }
        else
        {
            _ = services.AddOptions<AdministrativeSmtpOptions>()
                .Bind(smtpSection)
                .ValidateOnStart();
            _ = services.AddSingleton<IValidateOptions<AdministrativeSmtpOptions>, AdministrativeSmtpOptionsValidator>();
            _ = services.AddSingleton<IAdministrativeNotifier, AdministrativeSmtpNotifier>();
        }
        _ = services.AddScoped<VaultProvisioningProcessor>();
        _ = services.AddScoped<VaultProjectionReconciler>();
        _ = services.AddScoped<VaultOrphanDirectoryReconciler>();
        _ = services.AddScoped<VaultResidueAlertProcessor>();
        _ = services.AddHostedService<VaultProvisioningBackgroundService>();
        _ = services.AddHostedService<NoteIndexBackgroundService>();
        return services;
    }
}
