namespace BalanceHarness;

internal sealed partial class TowerWorkAccounting
{
    // Returned false values and APIs' existing error suppression are preserved.
    // Completion counts a selected API return, not filesystem completeness.
    public static T ObserveOperation<T>(string name, Func<T> action)
    {
        var owner = Active.Value;
        owner?.Record(name + "Attempted", 1);
        T result;
        try { result = action(); }
        catch { owner?.Record(name + "Failed", 1); throw; }
        owner?.Record(name + "Completed", 1);
        return result;
    }

    // Some existing consumers use SetLength/Flush. Preserve the FileStream
    // contract, open flags and inactive concrete type.
    public static FileStream OpenWriterLease(string path)
    {
        var owner = Active.Value;
        return ObserveOperation("writerLeaseAcquire", () => TowerNativeLeases.Enabled
            ? TowerNativeLeases.Open(path, owner is null ? null : new WriterLeaseLifetime(owner)) : owner is null
            ? new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose)
            : new ObservedLease(path, owner));
    }

    internal sealed class WriterLeaseLifetime(TowerWorkAccounting owner)
    {
        private int released;
        public void Release(Action release)
        {
            if (Interlocked.Exchange(ref released, 1) != 0) return;
            owner.Record("writerLeaseReleaseAttempted", 1);
            try { release(); }
            catch
            {
                owner.Record("writerLeaseReleaseFailed", 1);
                owner.Record("writerLeaseReleaseStateUnknown", 1);
                throw;
            }
            owner.Record("writerLeaseReleaseCompleted", 1);
        }
    }

    private sealed class ObservedLease(string path, TowerWorkAccounting owner)
        : FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose)
    {
        private readonly WriterLeaseLifetime lifetime = new(owner);
        protected override void Dispose(bool disposing)
        {
            // Finalization is outside explicit lifecycle observations. A base
            // constructor failure can occur before lifetime is initialized.
            if (!disposing || lifetime is null) base.Dispose(disposing);
            else lifetime.Release(() => base.Dispose(true));
        }
    }

    public static void RequireWriterLeaseHeld(string path)
        => TowerNativeLeases.Probe(path, () => RequireWriterLeaseHeldCore(path));
    private static void RequireWriterLeaseHeldCore(string path)
    {
        var owner = Active.Value;
        owner?.Record("writerLeaseProbeAttempted", 1);
        try
        {
            // Catch acquisition errors only: a failed Dispose is never evidence
            // that another owner holds the lease.
            IDisposable lease;
            try { lease = TowerCompactBundle.AcquireWriter(path); }
            catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33)
            {
                owner?.Record("writerLeaseProbeHeld", 1);
                owner?.Record("writerLeaseProbeCompleted", 1);
                return;
            }
            lease.Dispose();
            owner?.Record("writerLeaseProbeMissing", 1);
            throw new InvalidDataException("Missing enclosing registry/output ownership.");
        }
        catch { owner?.Record("writerLeaseProbeFailed", 1); throw; }
    }
}
