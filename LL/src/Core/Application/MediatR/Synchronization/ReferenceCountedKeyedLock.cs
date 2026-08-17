using System.Collections.Concurrent;

namespace Application.MediatR.Synchronization;

/// <summary>
/// A keyed async lock that retires an entry only after its holder and all waiters
/// have left. Retired entries cannot be acquired, preventing split-lock races.
/// </summary>
public sealed class ReferenceCountedKeyedLock<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Entry> _entries = new();

    public int EntryCount => _entries.Count;

    public async ValueTask<IDisposable> AcquireAsync(TKey key, CancellationToken cancellationToken)
    {
        while (true)
        {
            var entry = _entries.GetOrAdd(key, static _ => new Entry());
            lock (entry.Sync)
            {
                if (entry.Retired)
                    continue;
                entry.ReferenceCount++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken);
                return new Lease(this, key, entry);
            }
            catch
            {
                ReleaseReference(key, entry);
                throw;
            }
        }
    }

    private void Release(TKey key, Entry entry)
    {
        entry.Semaphore.Release();
        ReleaseReference(key, entry);
    }

    private void ReleaseReference(TKey key, Entry entry)
    {
        var retire = false;
        lock (entry.Sync)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0)
            {
                entry.Retired = true;
                retire = true;
            }
        }

        if (retire)
        {
            ((ICollection<KeyValuePair<TKey, Entry>>)_entries)
                .Remove(new KeyValuePair<TKey, Entry>(key, entry));
            entry.Semaphore.Dispose();
        }
    }

    private sealed class Entry
    {
        public object Sync { get; } = new();
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
        public bool Retired { get; set; }
    }

    private sealed class Lease(
        ReferenceCountedKeyedLock<TKey> owner,
        TKey key,
        Entry entry) : IDisposable
    {
        private ReferenceCountedKeyedLock<TKey>? _owner = owner;

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.Release(key, entry);
        }
    }
}

internal static class CharacterCommandLockRegistry
{
    internal static ReferenceCountedKeyedLock<Guid> Instance { get; } = new();
}
