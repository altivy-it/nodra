using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nodra.Backend.Application.Vaults;
using Nodra.Backend.Infra.Data.Postgres;
using Nodra.Notes.Worker.DependencyInjection;
using Nodra.Notes.Worker.Notifications;
using Xunit;

namespace Nodra.Notes.Worker.UnitTests.DependencyInjection;

[Trait("Category", "Unit")]
public sealed class WorkerPostgresHostOverrideTests
{
    [Fact]
    public void DevelopmentUsesConfiguredPostgresHostOverride()
    {
        using var host = CreateWorkerHost(Environments.Development);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        Assert.Contains("Host=postgres", context.Database.GetDbConnection().ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionIgnoresDevelopmentPostgresHostOverride()
    {
        using var host = CreateWorkerHost(Environments.Production);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        Assert.Contains("Host=localhost", context.Database.GetDbConnection().ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DevelopmentUsesNonSendingNotifierWhenSmtpIsNotConfigured()
    {
        using var host = CreateWorkerHost(Environments.Development, configureSmtp: false);

        _ = Assert.IsType<DevelopmentAdministrativeNotifier>(
            host.Services.GetRequiredService<IAdministrativeNotifier>());
    }

    [Fact]
    public void ProductionKeepsSmtpNotifierWhenSmtpIsConfigured()
    {
        using var host = CreateWorkerHost(Environments.Production);

        _ = Assert.IsType<AdministrativeSmtpNotifier>(
            host.Services.GetRequiredService<IAdministrativeNotifier>());
    }

    private static IHost CreateWorkerHost(string environmentName, bool configureSmtp = true)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName,
            ContentRootPath = Path.GetTempPath()
        });
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AltivyPostgres"] = "Host=localhost;Port=5432;Database=altivy;Username=runtime;Password=test-only",
            ["Database:PostgresHostOverride"] = "postgres",
            ["VaultStorage:Root"] = Path.GetTempPath(),
        };
        if (configureSmtp)
        {
            settings["ResidueAlerts:Smtp:Host"] = "smtp.example.test";
            settings["ResidueAlerts:Smtp:Port"] = "587";
            settings["ResidueAlerts:Smtp:Username"] = "worker-test";
            settings["ResidueAlerts:Smtp:Password"] = "test-only";
            settings["ResidueAlerts:Smtp:From"] = "altivy@example.test";
            settings["ResidueAlerts:Smtp:To"] = "operator@example.test";
        }

        _ = builder.Configuration.AddInMemoryCollection(settings);
        _ = builder.Services.AddVaultProvisioningWorker(builder.Configuration, builder.Environment);

        return builder.Build();
    }
}
