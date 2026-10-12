using Altivy.Backend.Application.Notes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altivy.Notes.Worker.Notes;

/// <summary>
/// Consome jobs duráveis de indexação de notas.
/// </summary>
public sealed class NoteIndexBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NoteIndexBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] TransientRetryDelays =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40),
        TimeSpan.FromSeconds(60)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var transientFailureCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessNextAsync(stoppingToken))
                {
                }

                transientFailureCount = 0;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Falha ao processar job de indexação de notas.");
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                var retryDelay = TransientRetryDelays[Math.Min(transientFailureCount, TransientRetryDelays.Length - 1)];
                transientFailureCount++;
                logger.LogWarning(
                    exception,
                    "Falha transitória ao acessar a fila PostgreSQL de indexação; nova tentativa em {RetryDelaySeconds}s.",
                    retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
                continue;
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private static bool IsTransient(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException || current is NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<NoteIndexJobProcessor>()
            .ProcessNextAsync(cancellationToken);
    }
}
