using System.Runtime.ExceptionServices;

namespace BalanceHarness;

internal sealed record NativeLeaseBudget(string Version, int MaxConcurrentLeases);

/// <summary>Read-only native proposal leases with a finite catalogue and scope-owned
/// cleanup. No bound on wall-clock duration or enclosing Python lease lifetimes.</summary>
internal sealed class TowerNativeLeases
{
    internal const string Version = "tower-proposal-native-leases-v1";
    private static readonly AsyncLocal<TowerNativeLeases?> Active = new();
    internal static bool Enabled => Active.Value is not null;
    private static readonly StringComparer Paths = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private readonly object gate = new();
    private readonly Dictionary<string, Entry> entries = new(Paths);
    private readonly string root, phase;
    private readonly NativeLeaseBudget budget;
    private string? probing;
    private int live, peak;
    private bool started, running, failed, complete;
    private sealed class Entry(string path, string role)
    {
        internal readonly string Path = path, Role = role;
        internal int Attempts, Acquired, Released, HeldConflicts, ReleaseFailures, ForcedCleanup;
        internal Lease? Current;
    }
    internal TowerNativeLeases(string root, string phase, NativeLeaseBudget budget)
    {
        Require(budget.Version == Version && phase is "native" or "nativeAudit" or "publication"
            && budget.MaxConcurrentLeases is >= 0 and <= 26 && (phase == "native" || budget.MaxConcurrentLeases == 0),
            "Invalid native lease contract.");
        this.root = Path.GetFullPath(root); this.phase = phase; this.budget = budget with { };
        if (phase == "native")
        {
            foreach (var name in new[] { Path.Combine(Path.GetDirectoryName(this.root)!, "complete-family-allocation"), this.root })
                Add(name, "EnclosingOwnerProbe");
            for (var n = 1; n <= 12; n++) foreach (var arm in new[] { "control", "candidate" })
                Add(Path.Combine(this.root, $"search/root-{n:D2}/{arm}/racing"), "NativeOwned");
        }
    }
    private void Add(string path, string role)
    {
        path = Path.GetFullPath(path) + ".writer.lock";
        Require(entries.TryAdd(path, new(path, role)), "Aliased native lease catalogue.");
    }
    internal void ProtectWorkerPaths(params string[] paths)
    {
        Require(!started, "Lease scope already started.");
        foreach (var entry in entries.Values)
        {
            var file = Path.GetFullPath(entry.Path);
            Require(paths.All(p => !Paths.Equals(file, Path.GetFullPath(p)) && !Inside(file, p) && !Inside(p, file)),
                "Native lease overlaps a protected worker path.");
            TowerProposalStudy.Unlinked(file);
        }
    }
    private static bool Inside(string p, string root) => Path.GetFullPath(p).StartsWith(
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar,
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
    internal static FileStream Open(string path, TowerWorkAccounting.WriterLeaseLifetime? lifetime) => Active.Value!.OpenCore(path, lifetime);
    private FileStream OpenCore(string path, TowerWorkAccounting.WriterLeaseLifetime? lifetime)
    {
        lock (gate)
        {
            var expectedConflict = false;
            try
            {
                Require(running && !failed, "Native lease scope unavailable.");
                var full = Path.GetFullPath(path);
                Require(Path.IsPathFullyQualified(path) && Paths.Equals(full, path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
                    && entries.TryGetValue(full, out _), "Undeclared native lease path.");
                var entry = entries[full];
                Require(entry.Attempts == 0 && (entry.Role == "EnclosingOwnerProbe" ? Paths.Equals(probing, full) : probing is null),
                    "Native lease operation or attempt allowance exhausted.");
                Require(live < budget.MaxConcurrentLeases, "Native lease handle allowance exhausted.");
                entry.Attempts++;
                TowerProposalStudy.Unlinked(full);
                Require(!Path.Exists(full) || entry.Role == "EnclosingOwnerProbe" && File.Exists(full) && new FileInfo(full).Length == 0,
                    "Existing or nonempty native lease.");
                TowerWorkAccounting.ObserveOperation("writerLeaseParentEnsure", () => Directory.CreateDirectory(Path.GetDirectoryName(full)!));
                TowerProposalStudy.Unlinked(full);
                Lease lease;
                try { lease = new Lease(this, entry, lifetime); }
                catch (IOException error) when (entry.Role == "EnclosingOwnerProbe" && (error.HResult & 0xffff) is 32 or 33)
                {
                    entry.HeldConflicts++; expectedConflict = true; throw;
                }
                entry.Current = lease; entry.Acquired++; live++; peak = Math.Max(peak, live);
                Require(lease.Length == 0 && !lease.CanWrite, "Native lease must remain empty and read-only.");
                return lease;
            }
            catch (Exception error)
            {
                if (!expectedConflict)
                {
                    failed = true;
                    // Only a conflict from the lease open itself proves ownership.
                    if (error is IOException && (error.HResult & 0xffff) is 32 or 33)
                        throw new InvalidDataException("Non-acquisition native lease conflict.", error);
                }
                throw;
            }
        }
    }
    internal static void Probe(string path, Action action)
    {
        if (Active.Value is not { } owner) { action(); return; }
        lock (owner.gate)
        {
            try
            {
                var full = Path.GetFullPath(path) + ".writer.lock";
                Require(owner.running && !owner.failed && owner.probing is null && owner.entries.TryGetValue(full, out var entry)
                    && entry.Role == "EnclosingOwnerProbe" && entry.Attempts == 0, "Undeclared native lease probe.");
                owner.probing = full; action();
                Require(owner.entries[full].HeldConflicts == 1, "Native lease probe did not establish enclosing ownership.");
            }
            catch { owner.failed = true; throw; }
            finally { owner.probing = null; }
        }
    }
    internal async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        Require(!started && Active.Value is null, "Native lease scope cannot be reused or nested.");
        started = running = true; Active.Value = this;
        Exception? original = null;
        try { return await action(); }
        catch (Exception error) { original = error; failed = true; throw; }
        finally
        {
            Exception? cleanup = null;
            lock (gate)
            {
                foreach (var entry in entries.Values.Where(e => e.Current is not null))
                {
                    failed = true; entry.ForcedCleanup++;
                    try { entry.Current!.Dispose(); } catch (Exception error) { cleanup ??= error; }
                }
                try
                {
                    Require(!failed && live == 0, "Incomplete native lease lifetime.");
                    foreach (var entry in entries.Values.Where(e => e.Acquired > 0))
                    { TowerProposalStudy.Unlinked(entry.Path); Require(!Path.Exists(entry.Path), "Native lease reappeared after release."); }
                    complete = true;
                }
                catch (Exception error) { failed = true; cleanup ??= error; }
                running = false;
            }
            Active.Value = null;
            if (cleanup is not null)
            {
                if (original is not null) original.Data["NativeLeaseCleanupError"] = cleanup.ToString();
                else ExceptionDispatchInfo.Capture(cleanup).Throw();
            }
        }
    }
    internal object Snapshot()
    {
        lock (gate) return new { version = Version, studyRoot = root, phase, budget,
            outcome = complete ? "Complete" : "FailedOrIncomplete", maxLeaseBytes = 0, acceptedWriteBytes = 0,
            currentOwnedHandles = live, peakOwnedHandles = peak, failedOrUnknown = failed,
            entries = entries.Values.Select(e => new { path = e.Path, role = e.Role, maxAttempts = 1,
                attempts = e.Attempts, acquired = e.Acquired, released = e.Released, heldConflicts = e.HeldConflicts,
                releaseFailures = e.ReleaseFailures, forcedCleanup = e.ForcedCleanup }).ToArray(),
            scope = "DeclaredNativeLeaseHandlesOnly", enclosingOwnerLifetimeBounded = false, wallClockBounded = false,
            filesystemConfinement = false, wholeProcessCoverage = false, usableForAdmission = false };
    }
    private sealed class Lease(TowerNativeLeases owner, Entry entry, TowerWorkAccounting.WriterLeaseLifetime? lifetime)
        : FileStream(entry.Path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose)
    {
        private bool closed;
        private void Reject()
        {
            lock (owner.gate) owner.failed = true;
            throw new NotSupportedException("Native lease writes and resizing are forbidden.");
        }
        public override void SetLength(long value) => Reject();
        public override void WriteByte(byte value) => Reject();
        public override void Write(byte[] buffer, int offset, int count) => Reject();
        public override void Write(ReadOnlySpan<byte> buffer) => Reject();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        { Reject(); return Task.CompletedTask; }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        { Reject(); return ValueTask.CompletedTask; }
        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback? callback, object? state)
        { Reject(); return null!; }
        public override ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        protected override void Dispose(bool disposing)
        {
            if (!disposing || owner is null) { base.Dispose(disposing); return; }
            lock (owner.gate)
            {
                if (closed) return;
                closed = true;
                try
                {
                    Exception? invalid = null;
                    try { Require(base.Length == 0, "Changed native lease length."); } catch (Exception error) { invalid = error; }
                    void Close() => base.Dispose(true);
                    if (lifetime is null) Close(); else lifetime.Release(Close);
                    if (invalid is not null) ExceptionDispatchInfo.Capture(invalid).Throw();
                    TowerProposalStudy.Unlinked(entry.Path);
                    Require(!Path.Exists(entry.Path), "Native lease release is unresolved.");
                    entry.Released++; owner.live--; entry.Current = null;
                }
                catch { owner.failed = true; entry.ReleaseFailures++; throw; }
            }
        }
    }
}
