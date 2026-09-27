using Altivy.Notes.Api.Authorization;
using Altivy.Notes.Api.Vaults;
using Altivy.Backend.Application.Notes;
using Altivy.Backend.Application.Vaults;
using Altivy.Backend.Infra.Data.Postgres;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Altivy.Notes.Api.DependencyInjection;

/// <summary>
/// Registra autenticação, autorização e integrações externas da API.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona os serviços necessários para validar tokens e consultar decisões UMA.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <param name="environment">Ambiente de execução.</param>
    /// <returns>A coleção de serviços para composição adicional.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var keycloakOptions = new KeycloakAuthorizationOptions
        {
            Authority = configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/altivy",
            Audience = configuration["Keycloak:Audience"] ?? "altivy-api"
        };

        _ = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakOptions.Authority;
                options.Audience = keycloakOptions.Audience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        _ = services.AddSingleton(keycloakOptions);
        var postgresConnectionString = configuration.GetConnectionString("AltivyPostgres");
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:AltivyPostgres é obrigatória fora de Development.");
            }

            _ = services.AddSingleton<IVaultReader, LabVaultReader>();
        }
        else
        {
            _ = services.AddPostgresData(postgresConnectionString);
            _ = services.AddScoped<IVaultReader>(provider =>
                provider.GetRequiredService<PostgresVaultReader>());
            _ = services.AddScoped<IVaultProvisioningStore, PostgresVaultProvisioningStore>();
        }
        _ = services.AddScoped<GetVaultQueryHandler>();
        _ = services.AddScoped<ListVaultCandidatesQueryHandler>();
        var vaultRoot = configuration["VaultStorage:Root"];
        if (string.IsNullOrWhiteSpace(vaultRoot))
        {
            vaultRoot = Path.Combine(environment.ContentRootPath, "vaults");
        }

        _ = services.AddSingleton<INoteStore>(new FileSystemNoteStore(vaultRoot));
        _ = services.AddSingleton<IFolderStore>(new FileSystemFolderStore(vaultRoot));
        _ = services.AddSingleton<IVaultFileSystemProvisioner>(
            new FileSystemVaultProvisioner(vaultRoot));
        _ = services.AddScoped<CreateNoteCommandHandler>();
        _ = services.AddScoped<DeleteEmptyFolderCommandHandler>();
        _ = services.AddScoped<EnsureFolderCommandHandler>();
        _ = services.AddScoped<ListFolderChildrenQueryHandler>();
        _ = services.AddScoped<GetNoteQueryHandler>();
        _ = services.AddScoped<ListNotesQueryHandler>();
        _ = services.AddScoped<UpdateNoteCommandHandler>();
        _ = services.AddScoped<DeleteNoteCommandHandler>();
        _ = services.AddScoped<MoveNoteCommandHandler>();
        _ = services.AddScoped<MoveFolderCommandHandler>();
        _ = services.AddHttpClient<IAuthorizationDecisionService, KeycloakAuthorizationDecisionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        _ = services.AddAuthorization();
        _ = services.AddSignalR();
        return services;
    }
}
