using System.Threading;
using System.Threading.Tasks;

public partial class World
{
    [System.NonSerialized] private WorldWorkLifetime runtimeWorkLifetime;
    private WorldWorkLifetime workLifetime => LazyInitializer.EnsureInitialized(ref runtimeWorkLifetime, () => new WorldWorkLifetime());
    private CancellationTokenSource taskTokenSource => workLifetime.Cancellation;
    // Snapshot application intentionally pauses gameplay with disabled=true.
    // Lifetime validity is separate from that simulation pause.
    private bool IsWorkSessionCurrent(int session) => workLifetime.IsCurrent(session);
    private bool HasPendingWorldWork => GenerationBusy || chunkUpdateInProgress || workLifetime.HasActiveWorkers;

    private async Task WaitForChunkRenderingAsync()
    {
        while (chunkUpdateInProgress || workLifetime.HasActiveWorkers)
        {
            worldCancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        worldCancellationToken.ThrowIfCancellationRequested();
    }
}
