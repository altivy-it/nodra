using Altivy.Backend.Domain.Notes;

namespace Altivy.Backend.Application.Notes;

/// <summary>
/// Processa um job de indexação com lease e retry duráveis.
/// </summary>
public sealed class NoteIndexJobProcessor(
    INoteIndexJobStore jobStore,
    INoteStore noteStore,
    INoteProjectionStore projectionStore,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Reivindica e processa o próximo job elegível.
    /// </summary>
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var job = await jobStore.ClaimNextAsync(now, LeaseDuration, cancellationToken);
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

            await jobStore.CompleteAsync(job, null, null, cancellationToken);
        }
        catch (IOException)
        {
            await jobStore.CompleteAsync(job, now.Add(RetryDelay), "filesystem-read-failed", cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            await jobStore.CompleteAsync(job, now.Add(RetryDelay), "filesystem-access-denied", cancellationToken);
        }

        return true;
    }
}