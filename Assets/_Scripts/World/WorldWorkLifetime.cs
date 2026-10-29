using System;
using System.Threading;

// One owner for cancellation and stale-result epochs across world operations.
public sealed class WorldWorkLifetime
{
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
    private int version;
    private int workers;
    public CancellationTokenSource Cancellation => cancellation;
    public int Version => Volatile.Read(ref version);
    public bool HasActiveWorkers => Volatile.Read(ref workers) != 0;
    public bool IsCurrent(int capturedVersion) => capturedVersion == Version && !cancellation.IsCancellationRequested;
    public void Invalidate() { Interlocked.Increment(ref version); }
    // Acquire before scheduling, not inside the worker: teardown must also see
    // work which is queued but has not yet started on the thread pool.
    public IDisposable BeginWorker()
    {
        Interlocked.Increment(ref workers);
        return new WorkerLease(this);
    }
    private sealed class WorkerLease : IDisposable
    {
        private WorldWorkLifetime owner;
        public WorkerLease(WorldWorkLifetime owner) { this.owner = owner; }
        public void Dispose()
        {
            var previous = Interlocked.Exchange(ref owner, null);
            if (previous != null) Interlocked.Decrement(ref previous.workers);
        }
    }
}
