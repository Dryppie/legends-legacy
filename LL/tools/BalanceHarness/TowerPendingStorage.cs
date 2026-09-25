using System.Runtime.ExceptionServices;

namespace BalanceHarness;

internal sealed record PendingFileBudget(string Path, string Destination, long MaxBytes, int MaxCreates);
internal sealed record PendingStorageBudget(IReadOnlyList<PendingFileBudget> Files, long MaxLiveBytes, long MaxTotalWrittenBytes);

/// <summary>Explicit diagnostic scope for selected native pending writers; not filesystem confinement.</summary>
internal sealed class TowerPendingStorage
{
    internal const string Version = "tower-native-pending-storage-v1";
    private static readonly AsyncLocal<TowerPendingStorage?> Active = new();
    private static readonly StringComparer Paths = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private readonly object gate = new();
    private readonly Dictionary<string, Entry> entries = new(Paths);
    private readonly long maxLive, maxTotal;
    private readonly TowerProposalPendingFamilies? families;
    private readonly List<string> protectedPaths = [];
    private long live, total, peak, published, deleted;
    private bool started, running, failed, complete;
    private sealed class Entry(PendingFileBudget budget)
    {
        internal readonly PendingFileBudget Budget = budget;
        internal int Creates;
        internal Writer? Current;
    }

    internal TowerPendingStorage(PendingStorageBudget budget, bool allowEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(budget);
        Require(budget.Files is not null && (budget.Files.Count > 0 || allowEmpty && budget.MaxLiveBytes == 0 && budget.MaxTotalWrittenBytes == 0) && budget.MaxLiveBytes >= 0
            && budget.MaxTotalWrittenBytes >= 0, "Invalid pending storage declaration.");
        maxLive = budget.MaxLiveBytes; maxTotal = budget.MaxTotalWrittenBytes;
        var destinations = new HashSet<string>(Paths);
        foreach (var file in budget.Files!)
        {
            Require(file is not null && file.MaxBytes >= 0 && file.MaxCreates > 0, "Invalid pending file declaration.");
            var source = Absolute(file!.Path); var target = Absolute(file.Destination);
            Require(!Paths.Equals(source, target) && destinations.Add(target)
                && entries.TryAdd(source, new(file with { Path = source, Destination = target })), "Aliased pending declaration.");
        }
        Require(!destinations.Overlaps(entries.Keys), "Pending sources and destinations overlap.");
        // Neither a declared file nor a destination may be an ancestor of another.
        var paths = entries.Keys.Concat(destinations).ToArray();
        Require(!paths.Any(p => paths.Any(q => !Paths.Equals(p, q) && Inside(q, p))), "Nested pending file declarations.");
    }
    private TowerPendingStorage(PendingStorageBudget budget, TowerProposalPendingFamilies families) : this(budget, allowEmpty: true)
    { this.families = families; }
    internal static TowerPendingStorage ForFamilies(TowerProposalPendingFamilies families) => new(families.FixedBudget(), families);
    private static string Absolute(string path)
    {
        Require(!string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path), "Pending paths must be absolute.");
        var full = Path.GetFullPath(path);
        Require(Paths.Equals(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), full), "Pending paths must be canonical.");
        Require(Path.GetFileName(full).Length > 0 && full.IndexOf(':', OperatingSystem.IsWindows() ? 2 : 0) < 0, "Invalid pending file path.");
        Require(!full.StartsWith(@"\\?\", StringComparison.Ordinal) && !full.StartsWith(@"\\.\", StringComparison.Ordinal), "Device pending paths are not supported.");
        foreach (var part in full[Path.GetPathRoot(full)!.Length..].Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            var stem = part.Split('.')[0].ToUpperInvariant();
            Require(part.Length > 0 && part[^1] is not (' ' or '.') && part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && stem is not ("CON" or "PRN" or "AUX" or "NUL")
                && !(stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                    && stem[3] is >= '1' and <= '9'), "Aliased or invalid pending path segment.");
        }
        return full;
    }
    private static bool Inside(string path, string folder) => path.StartsWith(
        Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar, Comparison);
    internal void ProtectWorkerPaths(params string[] protectedPaths)
    {
        Require(!started, "Cannot change an active pending scope.");
        this.protectedPaths.AddRange(protectedPaths.Select(Path.GetFullPath));
        // Protect the entire dynamic directory before any writer is opened.
        var paths = entries.Values.SelectMany(e => new[] { e.Budget.Path, e.Budget.Destination });
        if (families?.Phase == "native") paths = paths.Append(Path.Combine(families.Root, "study"));
        foreach (var path in paths)
        {
            TowerProposalStudy.Unlinked(path);
            Require(protectedPaths.All(p => !Paths.Equals(path, Path.GetFullPath(p))
                && !Inside(path, Path.GetFullPath(p)) && !Inside(Path.GetFullPath(p), path)),
                "Pending declaration overlaps a protected worker path.");
        }
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
    private T Call<T>(Func<T> action)
    {
        lock (gate)
        {
            try { Require(running && !failed, "Pending storage is unavailable."); return action(); }
            catch { failed = true; throw; }
        }
    }
    private void Call(Action action) => Call(() => { action(); return 0; });
    internal async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        Require(!started && Active.Value is null, "Pending storage scope cannot be reused or nested.");
        started = running = true;
        Active.Value = this;
        Exception? original = null;
        try { return await action(); }
        catch (Exception error) { original = error; failed = true; throw; }
        finally
        {
            Exception? cleanup = null;
            lock (gate)
            {
                foreach (var entry in entries.Values)
                    try { entry.Current?.Dispose(); }
                    catch (Exception error) { cleanup ??= error; }
                try
                {
                    if (original is null && cleanup is null)
                    {
                        Require(!failed && live == 0 && entries.Values.All(e => e.Current is null), "Unresolved pending storage.");
                        foreach (var path in entries.Keys)
                        {
                            TowerProposalStudy.Unlinked(path);
                            Require(!Path.Exists(path), "Pending path reappeared after release.");
                        }
                        complete = true;
                    }
                }
                catch (Exception error) { failed = true; cleanup = error; }
                running = false;
            }
            Active.Value = null;
            if (cleanup is not null)
            {
                if (original is not null) original.Data["PendingStorageCleanupError"] = cleanup.ToString();
                else ExceptionDispatchInfo.Capture(cleanup).Throw();
            }
        }
    }

    internal object Snapshot()
    {
        lock (gate)
        {
            if (families is not null) return new { version = TowerProposalPendingFamilies.StorageVersion,
                outcome = complete ? "Complete" : "FailedOrIncomplete", studyRoot = families.Root, phase = families.Phase, familyBudget = families.Budget,
                maxLiveBytes = maxLive, maxTotalWrittenBytes = maxTotal, acceptedWriteBytes = total,
                trackedLiveBytes = live, peakTrackedLiveBytes = peak, publishedBytes = published, deletedBytes = deleted,
                failedProgressMayBeUnknown = failed, contracts = entries.Values.Select(e => e.Budget).ToArray(),
                creates = entries.Values.Select(e => new { path = e.Budget.Path, count = e.Creates }).ToArray(),
                scope = "DeclaredInstrumentedPendingWritersOnly", filesystemConfinement = false,
                wholeProcessCoverage = false, usableForAdmission = false };
            return new { version = Version, outcome = complete ? "Complete" : "FailedOrIncomplete",
            maxLiveBytes = maxLive, maxTotalWrittenBytes = maxTotal, acceptedWriteBytes = total,
            trackedLiveBytes = live, peakTrackedLiveBytes = peak, publishedBytes = published, deletedBytes = deleted,
            failedProgressMayBeUnknown = failed, contracts = entries.Values.Select(e => e.Budget).ToArray(),
            creates = entries.Values.Select(e => new { path = e.Budget.Path, count = e.Creates }).ToArray(),
            scope = "DeclaredInstrumentedPendingWritersOnly", filesystemConfinement = false,
            wholeProcessCoverage = false, usableForAdmission = false };
        }
    }

    internal static Stream Open(string path, Func<Stream> create) => Active.Value is { } owner ? owner.OpenCore(path, create) : create();
    private Stream OpenCore(string path, Func<Stream> create) => Call(() => {
        if (families is not null)
        {
            path = Absolute(path);
            if (!entries.ContainsKey(path))
            {
                var budget = families.Add(path);
                Require(protectedPaths.All(p => !Paths.Equals(path, p) && !Inside(path, p) && !Inside(p, path)
                    && !Paths.Equals(budget.Destination, p) && !Inside(budget.Destination, p) && !Inside(p, budget.Destination)),
                    "Pending family overlaps a protected worker path.");
                TowerProposalStudy.Unlinked(budget.Destination);
                entries.Add(path, new(budget));
            }
        }
        Require(entries.TryGetValue(Path.GetFullPath(path), out var entry), "Undeclared pending writer.");
        Require(entry!.Current is null && entry.Creates < entry.Budget.MaxCreates, "Pending create allowance exhausted.");
        TowerProposalStudy.Unlinked(entry.Budget.Path);
        Require(!Path.Exists(entry.Budget.Path), "Pending path already exists.");
        entry.Creates++;
        var inner = create();
        try
        {
            Require(inner.CanWrite && inner.Length == 0 && inner.Position == 0, "Pending writer must start empty.");
            return (Stream)(entry.Current = new Writer(this, entry, inner));
        }
        catch (Exception error)
        {
            try { inner.Dispose(); } catch (Exception close) { error.Data["PendingOpenCleanupError"] = close.ToString(); }
            throw;
        }
    });
    internal static void Sync(Stream stream)
    {
        if (stream is Writer writer) writer.Sync();
        else ((FileStream)stream).Flush(true);
    }
    private Writer Closed(string path)
    {
        Require(entries.TryGetValue(Path.GetFullPath(path), out var entry) && entry.Current is not null, "Undeclared pending release.");
        var writer = entry!.Current!;
        Require(writer.Closed, "Cannot publish or delete an open pending writer.");
        writer.Verify();
        return writer;
    }
    private void Release(Writer writer, bool publish)
    {
        live -= writer.Size;
        if (publish) published = checked(published + writer.Size); else deleted = checked(deleted + writer.Size);
        writer.Entry.Current = null;
    }
    internal static void Move(string source, string destination, bool overwrite)
    {
        if (Active.Value is not { } owner) { File.Move(source, destination, overwrite); return; }
        owner.Call(() => {
            var writer = owner.Closed(source);
            Require(Paths.Equals(Path.GetFullPath(destination), writer.Entry.Budget.Destination), "Undeclared pending destination.");
            TowerProposalStudy.Unlinked(destination);
            File.Move(source, destination, overwrite);
            owner.Release(writer, true);
        });
    }
    internal static void Delete(string path)
    {
        if (Active.Value is not { } owner) { File.Delete(path); return; }
        owner.Call(() => { var writer = owner.Closed(path); File.Delete(path); owner.Release(writer, false); });
    }
    internal static void PublishDirectory(string source, string destination, Action<string, string> move)
    {
        if (Active.Value is not { } owner) { move(source, destination); return; }
        owner.Call(() => {
            source = Path.GetFullPath(source); destination = Path.GetFullPath(destination);
            TowerProposalStudy.Unlinked(source); TowerProposalStudy.Unlinked(destination);
            var paths = new List<string>(); var folders = new Stack<string>(); folders.Push(source);
            while (folders.TryPop(out var folder))
            {
                TowerProposalStudy.Unlinked(folder);
                foreach (var path in Directory.EnumerateFileSystemEntries(folder))
                {
                    TowerProposalStudy.Unlinked(path);
                    if (Directory.Exists(path)) folders.Push(path); else paths.Add(path);
                }
            }
            Require(paths.Count > 0, "Empty pending publication directory.");
            var writers = paths.Select(owner.Closed).ToArray();
            Require(writers.All(w => Paths.Equals(w.Entry.Budget.Destination,
                Path.Combine(destination, Path.GetRelativePath(source, w.Entry.Budget.Path)))), "Undeclared pending directory destination.");
            move(source, destination);
            Require(!Directory.Exists(source) && writers.All(w => File.Exists(w.Entry.Budget.Destination)), "Incomplete pending directory move.");
            foreach (var writer in writers) owner.Release(writer, true);
        });
    }

    private sealed class Writer(TowerPendingStorage owner, Entry entry, Stream inner) : Stream
    {
        internal readonly Entry Entry = entry;
        internal long Size { get; private set; }
        internal bool Closed { get; private set; }
        private string? hash;
        internal void Verify()
        {
            TowerProposalStudy.Unlinked(Entry.Budget.Path);
            Require(File.Exists(Entry.Budget.Path) && new FileInfo(Entry.Budget.Path).Length == Size
                && HarnessJson.FileHash(Entry.Budget.Path) == hash, "Changed pending content or length.");
        }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !Closed;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (owner.gate)
            {
                try
                {
                    Require(owner.running && !owner.failed && !Closed, "Pending writer unavailable.");
                    Require(buffer.Length <= Entry.Budget.MaxBytes - Size && buffer.Length <= owner.maxLive - owner.live
                        && buffer.Length <= owner.maxTotal - owner.total, "Pending byte limit exceeded.");
                    inner.Write(buffer);
                    Size += buffer.Length; owner.live += buffer.Length; owner.total += buffer.Length;
                    owner.peak = Math.Max(owner.peak, owner.live);
                }
                catch { owner.failed = true; throw; }
            }
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => WriteAsync(buffer.AsMemory(offset, count), token).AsTask();
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            try { token.ThrowIfCancellationRequested(); Write(buffer.Span); return ValueTask.CompletedTask; }
            catch (Exception error) { lock (owner.gate) owner.failed = true; return ValueTask.FromException(error); }
        }
        public override void Flush() => owner.Call(inner.Flush);
        internal void Sync() => owner.Call(() => ((FileStream)inner).Flush(true));
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (owner.gate)
                {
                    if (!Closed)
                    {
                        Closed = true;
                        try
                        {
                            inner.Dispose();
                            if (owner.failed) return; // Failed write progress remains unknown, never a success claim.
                            Require(new FileInfo(Entry.Budget.Path).Length == Size, "Unknown pending write progress.");
                            TowerProposalStudy.Unlinked(Entry.Budget.Path);
                            hash = HarnessJson.FileHash(Entry.Budget.Path);
                        }
                        catch { owner.failed = true; throw; }
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
