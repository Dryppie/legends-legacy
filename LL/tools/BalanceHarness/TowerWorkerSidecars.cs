namespace BalanceHarness;

internal sealed record WorkerSidecarByteLimits(long Receipt, long Publication, long Persistence, long Total)
{
    internal void Validate()
    {
        if (Receipt < 0 || Publication < 0 || Persistence < 0 || Total < 0)
            throw new InvalidDataException("Invalid worker sidecar byte limits.");
    }
    internal long For(string role) => role switch {
        "receipt" => Receipt, "publication" => Publication, "persistence" => Persistence,
        _ => throw new InvalidDataException("Unknown worker sidecar role.")
    };
}

/// <summary>Three cooperative append-only writers; no OS confinement or external-path coverage.</summary>
internal sealed class TowerWorkerSidecars
{
    private readonly WorkerSidecarByteLimits limits;
    private readonly Dictionary<string, BoundedWriter> files = new(StringComparer.Ordinal);
    private long total;
    private bool failed;

    internal TowerWorkerSidecars(WorkerSidecarByteLimits limits) { limits.Validate(); this.limits = limits; }
    private void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
    private T Call<T>(Func<T> action)
    {
        try { return action(); }
        catch { failed = true; throw; }
    }
    private void Call(Action action) => Call(() => { action(); return 0; });

    internal Stream Open(string role, string path, Func<Stream> reserve) => Call(() => {
        _ = limits.For(role);
        Require(!failed && !files.ContainsKey(role), "Invalid worker sidecar reservation.");
        TowerProposalStudy.Unlinked(path);
        var writer = new BoundedWriter(this, role, path, reserve());
        files.Add(role, writer);
        return (Stream)writer;
    });

    internal static void Sync(Stream stream)
    {
        if (stream is BoundedWriter writer) writer.Sync();
        else ((FileStream)stream).Flush(true);
    }

    internal void Finish(Exception? original)
    {
        Exception? failure = null;
        foreach (var writer in files.Values)
        {
            try { writer.Dispose(); }
            catch (Exception error)
            {
                if (original is not null) original.Data["WorkerSidecarCleanupError." + writer.Role] = error.ToString();
                else if (failure is null) failure = error;
                else failure.Data["WorkerSidecarCleanupError." + writer.Role] = error.ToString();
            }
        }
        if (original is not null) return;
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        Call(() => {
            Require(!failed && files.Count == 3, "Incomplete worker sidecar storage.");
            foreach (var writer in files.Values)
            {
                TowerProposalStudy.Unlinked(writer.Path);
                var info = new FileInfo(writer.Path);
                Require(info.Exists && (info.Attributes & FileAttributes.Directory) == 0 && info.Length == writer.Size,
                    "Changed worker sidecar length.");
            }
        });
    }

    private sealed class BoundedWriter(TowerWorkerSidecars owner, string role, string path, Stream inner) : Stream
    {
        internal string Role => role;
        internal string Path => path;
        internal long Size { get; private set; }
        private bool closed;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !closed;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => owner.Call(() => {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            owner.Require(offset <= buffer.Length - count, "Invalid worker sidecar buffer.");
            owner.Require(!closed && !owner.failed, "Unavailable worker sidecar writer.");
            owner.Require(count <= owner.limits.For(role) - Size && count <= owner.limits.Total - owner.total,
                "Worker sidecar byte limit exceeded.");
            inner.Write(buffer, offset, count);
            Size = checked(Size + count);
            owner.total = checked(owner.total + count);
        });
        public override void Flush() => owner.Call(inner.Flush);
        internal void Sync() => owner.Call(() => ((FileStream)inner).Flush(true));
        protected override void Dispose(bool disposing)
        {
            if (disposing && !closed)
            {
                owner.Call(inner.Dispose);
                closed = true;
            }
            base.Dispose(disposing);
        }
    }
}
