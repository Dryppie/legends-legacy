namespace BalanceHarness;

internal sealed partial class TowerWorkAccounting
{
    // These are observed logical file lengths, not allocated disk blocks or a directory inventory.
    // Only explicitly instrumented files participate. In particular, unknown failed-write bytes
    // are never inferred from length/position (an overwrite can leave both unchanged).
    private sealed record TrackedFile(long Length, bool Scratch);
    private readonly Dictionary<string, TrackedFile> writtenFiles = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private long retainedBytes, scratchBytes;

    public static Stream WriteStream(Stream inner, string path, bool leaveOpen = false, bool scratch = false)
        => Active.Value is { } active ? new CountedWriteStream(inner, path, active, leaveOpen, scratch) : inner;

    // Validate a diagnostic pending declaration before invoking the create factory.
    public static Stream OpenWrite(string path, Func<Stream> create, bool scratch = false)
        => WriteStream(scratch ? TowerPendingStorage.Open(path, create) : create(), path, scratch: scratch);

    public static void ObserveExistingFile(string path, bool scratch = false)
    {
        if (Active.Value is not { } active) return;
        active.ObserveLength(path, scratch, () => File.Exists(path) ? new FileInfo(path).Length : 0);
    }

    // Keep the native durable flush, including its failures, at the original publication boundary.
    public static void FlushToDisk(Stream stream)
    {
        if (stream is CountedWriteStream counted) counted.FlushToDisk();
        else TowerPendingStorage.Sync(stream);
    }

    public static void MoveFile(string source, string destination, bool overwrite)
    {
        var active = Active.Value;
        try { TowerPendingStorage.Move(source, destination, overwrite); }
        catch { active?.Record("fileMoveFailures", 1); throw; }
        if (active is null) return;
        lock (active.counters)
        {
            if (active.writtenFiles.Remove(Path.GetFullPath(source), out var moved))
            {
                var target = Path.GetFullPath(destination);
                active.AdjustStorage(moved, -1);
                if (active.writtenFiles.TryGetValue(target, out var replaced))
                {
                    active.Record("trackedReplacedBytes", replaced.Length);
                    active.AdjustStorage(replaced, -1);
                }
                var published = moved with { Scratch = false };
                active.writtenFiles[target] = published;
                active.AdjustStorage(published, 1);
                active.Record("trackedFileMoves", 1);
                active.UpdateStorage();
            }
            else active.Record("untrackedFileMoves", 1);
        }
    }

    public static void DeleteFile(string path)
    {
        var active = Active.Value;
        try { TowerPendingStorage.Delete(path); }
        catch { active?.Record("fileDeleteFailures", 1); throw; }
        if (active is null) return;
        lock (active.counters)
        {
            if (active.writtenFiles.Remove(Path.GetFullPath(path), out var removed))
            {
                active.Record("trackedDeletedBytes", removed.Length);
                active.Record("trackedFileDeletes", 1);
                active.AdjustStorage(removed, -1);
                active.UpdateStorage();
            }
            else active.Record("untrackedFileDeletes", 1);
        }
    }

    private void ObserveLength(string path, bool scratch, Func<long> length)
    {
        try
        {
            var observed = length();
            if (observed < 0) throw new IOException("Negative observed length.");
            lock (counters)
            {
                var fullPath = Path.GetFullPath(path);
                if (writtenFiles.TryGetValue(fullPath, out var previous)) AdjustStorage(previous, -1);
                var current = new TrackedFile(observed, scratch);
                writtenFiles[fullPath] = current;
                AdjustStorage(current, 1);
                UpdateStorage();
            }
        }
        catch (Exception)
        {
            // Accounting must not hide the writer's original exception. Missing observations
            // remain explicit, and no receipt from this collector can be used for admission.
            Record("storageLengthObservationFailures", 1);
        }
    }

    private void AdjustStorage(TrackedFile file, int direction)
    {
        if (file.Scratch) scratchBytes = checked(scratchBytes + direction * file.Length);
        else retainedBytes = checked(retainedBytes + direction * file.Length);
    }

    private void UpdateStorage()
    {
        var retained = retainedBytes;
        var scratch = scratchBytes;
        var combined = checked(retained + scratch);
        counters["trackedRetainedBytes"] = retained;
        counters["trackedScratchBytes"] = scratch;
        counters["trackedCombinedBytes"] = combined;
        counters["peakTrackedRetainedBytes"] = Math.Max(counters.GetValueOrDefault("peakTrackedRetainedBytes"), retained);
        counters["peakTrackedScratchBytes"] = Math.Max(counters.GetValueOrDefault("peakTrackedScratchBytes"), scratch);
        counters["peakTrackedCombinedBytes"] = Math.Max(counters.GetValueOrDefault("peakTrackedCombinedBytes"), combined);
    }

    private sealed class CountedWriteStream : Stream
    {
        private readonly Stream inner;
        private readonly string path;
        private readonly TowerWorkAccounting owner;
        private readonly bool leaveOpen, scratch;
        private bool disposed;
        internal CountedWriteStream(Stream inner, string path, TowerWorkAccounting owner, bool leaveOpen, bool scratch)
        {
            this.inner = inner; this.path = Path.GetFullPath(path); this.owner = owner;
            this.leaveOpen = leaveOpen; this.scratch = scratch;
            Observe();
        }
        private void Observe() => owner.ObserveLength(path, scratch, () => inner.Length);
        private void Begin() => owner.Record("writeOperationsAttempted", 1);
        private void Written(int count)
        { owner.Record("applicationWriteBytes." + Role(path), count); owner.Record("writeOperationsCompleted", 1); }
        private void Failed(int requested)
        {
            owner.Record("writeOperationsFailed", 1);
            owner.Record("failedWriteRequestedBytes." + Role(path), requested);
            owner.Record("failedWriteBytesUnknown", 1);
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Begin();
            try { inner.Write(buffer); Written(buffer.Length); }
            catch { Failed(buffer.Length); throw; }
            finally { Observe(); }
        }
        public override void WriteByte(byte value)
        {
            Begin();
            try { inner.WriteByte(value); Written(1); }
            catch { Failed(1); throw; }
            finally { Observe(); }
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => WriteAsync(buffer.AsMemory(offset, count), token).AsTask();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            Begin();
            try { await inner.WriteAsync(buffer, token); Written(buffer.Length); }
            catch { Failed(buffer.Length); throw; }
            finally { Observe(); }
        }
        private void FlushWith(Action flush)
        {
            owner.Record("flushOperationsAttempted", 1);
            try { flush(); owner.Record("flushOperationsCompleted", 1); }
            catch { owner.Record("flushOperationsFailed", 1); throw; }
            finally { Observe(); }
        }
        public override void Flush() => FlushWith(inner.Flush);
        internal void FlushToDisk() => FlushWith(() => TowerPendingStorage.Sync(inner));
        public override async Task FlushAsync(CancellationToken token)
        {
            owner.Record("flushOperationsAttempted", 1);
            try { await inner.FlushAsync(token); owner.Record("flushOperationsCompleted", 1); }
            catch { owner.Record("flushOperationsFailed", 1); throw; }
            finally { Observe(); }
        }
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) { try { inner.SetLength(value); } finally { Observe(); } }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                Observe();
                if (!leaveOpen)
                {
                    try { inner.Dispose(); owner.Record("writeStreamsClosed", 1); }
                    catch { owner.Record("writeStreamCloseFailures", 1); throw; }
                }
            }
            base.Dispose(disposing);
        }
    }
}
