using System.IO.Enumeration;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace BalanceHarness;

/// <summary>Complete ledger discovery; ordinary files need no allocated path or FileSystemInfo.</summary>
internal static class TowerHistoryRegistry
{
    internal const int MaximumDirectories = 2_000_000;

    internal static bool Include(ReadOnlySpan<char> name, FileAttributes attributes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Linked history artifact.");
        return (attributes & FileAttributes.Directory) != 0
            || name.SequenceEqual("seed-ledger.json") || name.SequenceEqual("prior-seed-ledger.json")
            || name.SequenceEqual("history-input.json");
    }

    internal static HashSet<string> Scan(string root, string excludedStudy, CancellationToken ct,
        int maximumDirectories = MaximumDirectories, int maximumConcurrency = 4)
    {
        if (maximumDirectories is < 1 or > MaximumDirectories)
            throw new ArgumentOutOfRangeException(nameof(maximumDirectories));
        if (maximumConcurrency is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrency));
        using var timing = TowerPerformanceTrace.Measure("history.registry");
        var found = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        using var queue = new BlockingCollection<string>(); queue.Add(Path.GetFullPath(root));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        excludedStudy = Path.GetFullPath(excludedStudy);
        var directories = 0; var pending = 1; long entries = 0, files = 0;
        ExceptionDispatchInfo? failure = null;
        var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = false,
            AttributesToSkip = 0, ReturnSpecialDirectories = false, BufferSize = 64 * 1024 };
        void Worker()
        {
            try
            {
                foreach (var path in queue.GetConsumingEnumerable(stop.Token))
                {
                    long localEntries = 0, localFiles = 0;
                    try
                    {
                        stop.Token.ThrowIfCancellationRequested();
                        if (Interlocked.Increment(ref directories) > maximumDirectories)
                            throw new InvalidDataException("History directory bound exceeded.");
                        if (string.Equals(path, excludedStudy, StringComparison.OrdinalIgnoreCase)) continue;
                        // A queued directory may have changed since its parent was read.
                        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                            throw new InvalidDataException("Linked history directory.");
                        var children = new FileSystemEnumerable<(string Path, bool Directory)>(path,
                            (ref FileSystemEntry entry) => (entry.ToFullPath(), entry.IsDirectory), options)
                        {
                            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                            {
                                localEntries++;
                                if (!entry.IsDirectory) localFiles++;
                                return Include(entry.FileName, entry.Attributes, stop.Token);
                            }
                        };
                        foreach (var child in children)
                        {
                            stop.Token.ThrowIfCancellationRequested();
                            if (child.Directory) { Interlocked.Increment(ref pending); queue.Add(child.Path); }
                            else found.TryAdd(child.Path, 0);
                        }
                    }
                    finally
                    {
                        Interlocked.Add(ref entries, localEntries); Interlocked.Add(ref files, localFiles);
                        // Pending includes queued and active directories, so completion cannot race an enqueue.
                        if (Interlocked.Decrement(ref pending) == 0) queue.CompleteAdding();
                    }
                }
            }
            catch (Exception error)
            {
                Interlocked.CompareExchange(ref failure, ExceptionDispatchInfo.Capture(error), null);
                stop.Cancel();
            }
        }
        try
        {
            // Dedicated bounded workers avoid depending on the caller's thread-pool availability.
            var workers = Enumerable.Range(0, maximumConcurrency).Select(_ => Task.Factory.StartNew(Worker,
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            Task.WhenAll(workers).GetAwaiter().GetResult();
            failure?.Throw();
            ct.ThrowIfCancellationRequested();
            return found.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            TowerPerformanceTrace.Count("directories", directories);
            TowerPerformanceTrace.Count("entries", entries);
            TowerPerformanceTrace.Count("files", files);
            TowerPerformanceTrace.Count("ledgers", found.Count);
        }
    }
}
