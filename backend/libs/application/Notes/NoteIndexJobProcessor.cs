using Nodra.Backend.Domain.Notes;

namespace Nodra.Backend.Application.Notes;

/// <summary>
/// Processa um job de indexação com lease e retry duráveis.
/// </summary>
public sealed class NoteIndexJobProcessor(
    INoteIndexJobStore jobStore,
    INoteStore noteStore,
    INoteProjectionStore projectionStore,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan s_leaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan s_retryDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reivindica e processa o próximo job elegível.
    /// </summary>
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var job = await jobStore.ClaimNextAsync(now, s_leaseDuration, cancellationToken);
        if (job is null)
        {
            return false;
        }

        try
        {
            if (job.Path is null)
            {
                var notes = await noteStore.ListAsync(job.VaultId, cancellationToken);
                foreach (var note in notes)
                {
                    await projectionStore.UpsertAsync(job.VaultId, note, cancellationToken);
                }
            }
            else
            {
                var note = await noteStore.FindAsync(
                    job.VaultId,
                    NotePath.Create(job.Path),
                    cancellationToken);
                if (note is not null)
                {
                    await projectionStore.UpsertAsync(job.VaultId, note, cancellationToken);
                }
                else
                {
                    await projectionStore.DeleteAsync(job.VaultId, NotePath.Create(job.Path), cancellationToken);
                }
            }

            _ = await jobStore.CompleteAsync(job, null, null, cancellationToken);
        }
        catch (IOException)
        {
            _ = await jobStore.CompleteAsync(job, now.Add(s_retryDelay), "filesystem-read-failed", cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            _ = await jobStore.CompleteAsync(job, now.Add(s_retryDelay), "filesystem-access-denied", cancellationToken);
        }

        return true;
    }
}
