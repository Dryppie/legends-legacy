namespace BalanceHarness;

/// <summary>
/// Opt-in single-writer accounting. Closed subtrees are immutable until the mandatory full hash audit;
/// active writes (including pending files) are reconciled at every existing storage boundary.
/// This is an execution cache, never evidence accepted by an archive verifier.
/// </summary>
internal sealed class TowerStorageAccountant
{
    public const string Mode = "owned-storage-v1";
    private readonly string root;
    private readonly long limit;
    private readonly HashSet<string> mutableFiles;
    private readonly Dictionary<string, string> sealedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> sealedDirectories = new(StringComparer.OrdinalIgnoreCase);
    private long sealedBytes;
    private string? activeDirectory;
    private TowerStorageAccountant? child;

    public TowerStorageAccountant(string root, long limit, IEnumerable<string> mutableFiles, CancellationToken token = default)
    {
        this.root = Path.GetFullPath(root); this.limit = limit;
        this.mutableFiles = mutableFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var timing = TowerPerformanceTrace.Measure("storage.initialize-audit");
        foreach (var file in Scan(this.root, token, sealedDirectories))
        {
            if (Path.GetDirectoryName(file.FullName) != this.root) sealedBytes = checked(sealedBytes + file.Length);
            if (!Mutable(file.FullName)) sealedHashes.Add(file.FullName, HarnessJson.FileHash(file.FullName));
        }
        Check(token);
    }

    internal string Root => root;
    public void AllowMetadata(string name)
    {
        if (Path.GetFileName(name) != name || string.IsNullOrWhiteSpace(name)) throw new InvalidDataException("Metadata must be a root file.");
        mutableFiles.Add(name); mutableFiles.Add(name + ".pending");
    }
    private bool Mutable(string path) => Path.GetDirectoryName(path) == root && mutableFiles.Contains(Path.GetFileName(path));

    public void BeginDirectory(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        path = Path.GetFullPath(path);
        if (activeDirectory is not null || child is not null || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || Path.Exists(path) || !sealedDirectories.Contains(Path.GetDirectoryName(path)!))
            throw new InvalidDataException("Storage writer requires one new owned subtree beneath an established directory.");
        // Check every existing ancestor before the writer creates anything through it.
        for (var parent = Path.GetDirectoryName(path); parent is not null && parent.Length >= root.Length; parent = Path.GetDirectoryName(parent))
            RejectLink(parent);
        // AcquireWriter holds a sibling DeleteOnClose lease, outside the child output itself.
        // Root leases are counted by the metadata scan; nested batch leases are counted below.
        if (Path.GetDirectoryName(path) == root) AllowMetadata(Path.GetFileName(path) + ".writer.lock");
        activeDirectory = path;
    }

    // Attach after child setup, before its first fight. Parent totals include this child exactly once.
    public void Attach(TowerStorageAccountant value)
    {
        if (activeDirectory != value.root || child is not null) throw new InvalidDataException("Storage child does not own the declared subtree.");
        child = value;
    }

    public long Check(CancellationToken token = default)
    {
        using var timing = TowerPerformanceTrace.Measure("storage.check-owned");
        token.ThrowIfCancellationRequested(); RejectLink(root);
        long bytes = sealedBytes;
        TowerPerformanceTrace.Count("storage.directories-visited");
        foreach (var file in new DirectoryInfo(root).EnumerateFiles())
        {
            token.ThrowIfCancellationRequested(); RejectLink(file.FullName);
            TowerPerformanceTrace.Count("storage.files-visited");
            if (!Mutable(file.FullName) && !sealedHashes.ContainsKey(file.FullName))
                throw new InvalidDataException("Unexpected campaign metadata: " + file.Name);
            bytes = checked(bytes + file.Length);
        }
        if (activeDirectory is not null && Path.GetDirectoryName(activeDirectory) != root)
        {
            var lease = new FileInfo(activeDirectory + ".writer.lock");
            if (lease.Exists)
            {
                RejectLink(lease.FullName); TowerPerformanceTrace.Count("storage.files-visited");
                bytes = checked(bytes + lease.Length);
            }
        }
        if (child is not null) bytes = checked(bytes + child.Check(token));
        else if (activeDirectory is not null)
        {
            RejectLink(Path.GetDirectoryName(activeDirectory)!);
            if (Directory.Exists(activeDirectory))
                foreach (var file in Scan(activeDirectory, token)) bytes = checked(bytes + file.Length);
        }
        if (bytes > limit) throw new InvalidDataException("Owned campaign storage limit reached at a batch/chunk boundary.");
        return bytes;
    }

    public void SealDirectory(CancellationToken token = default)
    {
        using var timing = TowerPerformanceTrace.Measure("storage.seal-subtree");
        Check(token);
        if (activeDirectory is null) throw new InvalidDataException("No active storage subtree.");
        // A nested campaign was independently audited by Finish. Copy its durable expectations,
        // including its mutable top-level outputs, rather than rescanning its historical batches.
        if (child is not null)
        {
            child.Audit(token);
            foreach (var pair in child.sealedHashes) sealedHashes.Add(pair.Key, pair.Value);
            sealedDirectories.UnionWith(child.sealedDirectories);
            foreach (var file in new DirectoryInfo(child.root).EnumerateFiles())
                sealedHashes[file.FullName] = HarnessJson.FileHash(file.FullName);
            sealedBytes = checked(sealedBytes + child.Check(token));
        }
        else
            foreach (var file in Scan(activeDirectory, token, sealedDirectories))
            {
                sealedHashes.Add(file.FullName, HarnessJson.FileHash(file.FullName));
                sealedBytes = checked(sealedBytes + file.Length);
            }
        child = null; activeDirectory = null;
    }

    public void Audit(CancellationToken token = default)
    {
        using var timing = TowerPerformanceTrace.Measure("storage.final-audit");
        if (activeDirectory is not null) throw new InvalidDataException("Cannot seal a campaign with an active or failed writer.");
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long bytes = 0;
        foreach (var file in Scan(root, token, directories))
        {
            bytes = checked(bytes + file.Length);
            if (Mutable(file.FullName)) continue;
            if (!sealedHashes.TryGetValue(file.FullName, out var hash) || HarnessJson.FileHash(file.FullName) != hash)
                throw new InvalidDataException("Changed or unexpected sealed campaign artifact: " + file.FullName);
            seen.Add(file.FullName);
        }
        if (!seen.SetEquals(sealedHashes.Keys) || !directories.SetEquals(sealedDirectories))
            throw new InvalidDataException("Sealed campaign inventory changed.");
        if (bytes > limit) throw new InvalidDataException("Campaign storage cap exceeded during final audit.");
    }

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Linked campaign artifact: " + path);
    }

    private static IEnumerable<FileInfo> Scan(string path, CancellationToken token, HashSet<string>? directories = null)
    {
        var stack = new Stack<string>(); stack.Push(path);
        while (stack.TryPop(out var directory))
        {
            token.ThrowIfCancellationRequested(); RejectLink(directory);
            directories?.Add(directory); TowerPerformanceTrace.Count("storage.directories-visited");
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                token.ThrowIfCancellationRequested();
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked campaign artifact.");
                if ((entry.Attributes & FileAttributes.Directory) != 0) stack.Push(entry.FullName);
                else { TowerPerformanceTrace.Count("storage.files-visited"); yield return (FileInfo)entry; }
            }
        }
    }
}

// Explicitly activated by a newly prepared outer protocol. Never used by historical verification.
internal static class TowerStorageOwnership
{
    private static readonly AsyncLocal<TowerStorageAccountant?> Current = new();
    public static TowerStorageAccountant? Parent => Current.Value;
    public static IDisposable Activate(TowerStorageAccountant? owner)
    {
        var previous = Current.Value; Current.Value = owner;
        return new Scope(() => Current.Value = previous);
    }
    private sealed class Scope(Action close) : IDisposable { public void Dispose() => close(); }
}
