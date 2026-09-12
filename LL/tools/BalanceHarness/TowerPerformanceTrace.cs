using System.Diagnostics;

namespace BalanceHarness;

public sealed record TowerStageTiming(string Path, long Calls, double InclusiveMilliseconds,
    double ExclusiveMilliseconds, long Bytes);

/// <summary>Opt-in async-context-local timings; nested durations must not be added together.</summary>
internal sealed class TowerPerformanceTrace(Action<bool>? battleProgress = null)
{
    private static readonly AsyncLocal<TowerPerformanceTrace?> Active = new();
    private static readonly AsyncLocal<Frame?> CurrentFrame = new();
    private readonly Dictionary<string, Totals> totals = new(StringComparer.Ordinal);
    private readonly Action<bool>? onBattle = battleProgress;

    public IDisposable Activate()
    {
        var previous = Active.Value; var frame = CurrentFrame.Value;
        Active.Value = this; CurrentFrame.Value = null;
        return new OnDispose(() => { Active.Value = previous; CurrentFrame.Value = frame; });
    }
    public static IDisposable? Measure(string name, long bytes = 0) => Active.Value is { } trace
        ? new Frame(trace, name, bytes) : null;
    public static bool Enabled => Active.Value is not null;
    public static void BattleStarted() => Active.Value?.onBattle?.Invoke(false);
    public static void BattleCompleted() => Active.Value?.onBattle?.Invoke(true);
    public IReadOnlyList<TowerStageTiming> Snapshot()
    {
        lock (totals)
            return totals.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new TowerStageTiming(p.Key,
                p.Value.Calls, p.Value.Ticks * 1000d / Stopwatch.Frequency,
                p.Value.Exclusive * 1000d / Stopwatch.Frequency, p.Value.Bytes)).ToArray();
    }
    private sealed class Totals { public long Calls, Ticks, Exclusive, Bytes; }
    private sealed class OnDispose(Action action) : IDisposable { public void Dispose() => action(); }
    private sealed class Frame : IDisposable
    {
        private readonly TowerPerformanceTrace owner;
        private readonly Frame? parent;
        private readonly string path;
        private readonly long start = Stopwatch.GetTimestamp();
        private readonly long bytes;
        private long childTicks;
        private bool disposed;
        public Frame(TowerPerformanceTrace trace, string name, long byteCount)
        {
            owner = trace; parent = CurrentFrame.Value; bytes = byteCount;
            path = parent is null ? name : parent.path + "/" + name;
            CurrentFrame.Value = this;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            var elapsed = Stopwatch.GetTimestamp() - start;
            if (parent is not null) Interlocked.Add(ref parent.childTicks, elapsed);
            CurrentFrame.Value = parent;
            lock (owner.totals)
            {
                if (!owner.totals.TryGetValue(path, out var total)) owner.totals[path] = total = new();
                total.Calls++; total.Ticks += elapsed;
                total.Exclusive += Math.Max(0, elapsed - childTicks); total.Bytes += bytes;
            }
        }
    }
    // Only enabled during profiling; preserves the serializer's streaming write path.
    internal sealed class WriteStream(Stream inner) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => true;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() { using var timing = Measure("io.write-flush"); inner.Flush(); }
        public override void Write(byte[] buffer, int offset, int count)
        { using var timing = Measure("io.write", count); inner.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer)
        { using var timing = Measure("io.write", buffer.Length); inner.Write(buffer); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
    }
}
