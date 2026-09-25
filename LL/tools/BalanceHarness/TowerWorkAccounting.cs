using System.Text;
using System.Text.Json;

namespace BalanceHarness;

/// <summary>Opt-in counters at instrumented boundaries. A snapshot never claims whole-process coverage.</summary>
internal sealed partial class TowerWorkAccounting
{
    public const string Version = "tower-proposal-work-counters-v1";
    private static readonly AsyncLocal<TowerWorkAccounting?> Active = new();
    private readonly Dictionary<string, long> counters = new(StringComparer.Ordinal);
    public static bool Enabled => Active.Value is not null;
    public IDisposable Activate()
    {
        var previous = Active.Value;
        Active.Value = this;
        return new Restore(() => Active.Value = previous);
    }
    private sealed class Restore(Action action) : IDisposable
    {
        private bool done;
        public void Dispose() { if (!done) { done = true; action(); } }
    }
    public static void Add(string name, long count = 1) => Active.Value?.Record(name, count);
    private void Record(string name, long count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        lock (counters) counters[name] = checked(counters.GetValueOrDefault(name) + count);
    }
    public IReadOnlyDictionary<string, long> Snapshot()
    { lock (counters) return new SortedDictionary<string, long>(counters, StringComparer.Ordinal); }
    public object Receipt(string phase, string requestSha256, string producerSha256, bool succeeded)
    {
        if (!new[] { "native", "nativeAudit", "independentAudit", "publication" }.Contains(phase)
            || !TowerContractJson.Hash(requestSha256) || !TowerContractJson.Hash(producerSha256))
            throw new InvalidDataException("Unbound work receipt.");
        return new { version = Version, phase, requestSha256, producerSha256,
            outcome = succeeded ? "Complete" : "Failed", coverage = "InstrumentedOperationsOnly",
            counters = Snapshot(), wholeProcessCoverage = false, usableForAdmission = false };
    }
    internal static string Role(string path) => Path.GetFileName(path) switch {
        "files.json" => "manifest", "evidence-storage.json" => "metadata",
        _ when path.EndsWith(".jsonl", StringComparison.Ordinal) => "journal",
        _ when path.EndsWith(".gz", StringComparison.Ordinal) => "payload",
        _ when path.EndsWith(".json", StringComparison.Ordinal) => "json",
        _ => "other"
    };
    // Stream counters capture the collector when opened; later scopes cannot steal its reads.
    public static Stream ReadStream(Stream inner, string path, bool leaveOpen = false) => Active.Value is { } active
        ? new CountedStream(inner, n => active.Record("applicationReadBytes." + Role(path), n), leaveOpen) : inner;
    public static Stream ParseStream(Stream inner) => Active.Value is { } active
        ? new CountedStream(inner, n => active.Record("jsonInputBytes", n), true) : inner;
    public static StreamReader OpenText(string path, FileShare share = FileShare.Read) => new(
        ReadStream(new FileStream(path, FileMode.Open, FileAccess.Read, share), path), Encoding.UTF8, true);
    public static string ReadAllText(string path)
    {
        using var reader = OpenText(path);
        return reader.ReadToEnd();
    }
    public static IEnumerable<string> ReadLines(string path)
    {
        using var reader = OpenText(path);
        while (reader.ReadLine() is { } line) yield return line;
    }
    public static T Parse<T>(string json, JsonSerializerOptions? options = null)
    {
        Add("jsonParseAttempts");
        if (Enabled) Add("jsonInputBytes", Encoding.UTF8.GetByteCount(json));
        var value = JsonSerializer.Deserialize<T>(json, options);
        Add("jsonParseCompleted");
        return value!;
    }
    public static JsonDocument ParseDocument(string json)
    {
        Add("jsonParseAttempts");
        if (Enabled) Add("jsonInputBytes", Encoding.UTF8.GetByteCount(json));
        var value = JsonDocument.Parse(json);
        Add("jsonParseCompleted");
        return value;
    }
    // A generic decoder pass completes only when a nonempty read observes EOF.
    // This counts bytes returned by the decoder, not compressed-format authentication.
    public static Stream DecodeStream(Stream inner)
    {
        if (Active.Value is not { } active) return inner;
        active.Record("decodePassesStarted", 1);
        return new CountedStream(inner, n => active.Record("decodedBytesProcessed", n), true,
            () => active.Record("decodePassesCompleted", 1));
    }
    public static T Parse<T>(Stream stream, JsonSerializerOptions options)
    {
        Add("jsonParseAttempts");
        // No disposal of a caller-owned stream, including the inactive path.
        var counted = ParseStream(stream);
        var value = JsonSerializer.Deserialize<T>(counted, options);
        Add("jsonParseCompleted");
        return value!;
    }
    private sealed class CountedStream(Stream inner, Action<int> add, bool leaveOpen, Action? complete = null) : Stream
    {
        private bool completed;
        private void Record(int count, int requested)
        {
            add(count);
            if (count == 0 && requested > 0 && !completed)
            { completed = true; complete?.Invoke(); }
        }
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer) { var n = inner.Read(buffer); Record(n, buffer.Length); return n; }
        public override int ReadByte() { var n = inner.ReadByte(); Record(n < 0 ? 0 : 1, 1); return n; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        { var n = await inner.ReadAsync(buffer, token); Record(n, buffer.Length); return n; }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => ReadAsync(buffer.AsMemory(offset, count), token).AsTask();
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing && !leaveOpen) inner.Dispose(); base.Dispose(disposing); }
    }
}
