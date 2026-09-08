using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Infra.Data.Postgres;
using Altivy.Notes.Worker.Notifications;
using Altivy.Notes.Worker.Notes;
using Altivy.Notes.Worker.Observability;
using Altivy.Notes.Worker.Vaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Altivy.Notes.Worker.DependencyInjection;

/// <summary>
/// Registra os serviços necessários ao processamento de provisioning de vaults.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
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
        _ = services.AddOptions<AdministrativeSmtpOptions>()
            .Bind(configuration.GetSection(AdministrativeSmtpOptions.SectionName))
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<AdministrativeSmtpOptions>, AdministrativeSmtpOptionsValidator>();
        _ = services.AddSingleton<IAdministrativeNotifier, AdministrativeSmtpNotifier>();
        _ = services.AddScoped<VaultProvisioningProcessor>();
        _ = services.AddScoped<VaultProjectionReconciler>();
        _ = services.AddScoped<VaultOrphanDirectoryReconciler>();
        _ = services.AddScoped<VaultResidueAlertProcessor>();
        _ = services.AddHostedService<VaultProvisioningBackgroundService>();
        _ = services.AddHostedService<NoteIndexBackgroundService>();
        return services;
    }
}
