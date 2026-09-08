using Altivy.Backend.Application.Notes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altivy.Notes.Worker.Notes;

/// <summary>
/// Consome jobs duráveis de indexação de notas.
/// </summary>
public sealed class NoteIndexBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NoteIndexBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessNextAsync(stoppingToken))
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Falha ao processar job de indexação de notas.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<NoteIndexJobProcessor>()
            .ProcessNextAsync(cancellationToken);
    }
}