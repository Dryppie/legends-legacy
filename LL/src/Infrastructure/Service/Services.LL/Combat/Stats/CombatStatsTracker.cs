using Domain.Models.Combat;
using Services.LL.Interfaces;
using System.Collections.Concurrent;

namespace Services.LL.Combat.Stats;
public sealed class CombatStatsTracker : ICombatStatsTracker
{
    private readonly ConcurrentDictionary<string, EntityStatsDictionary> _entities = new();

    public void AddLogEntry(CombatLogEntry entry)
    {
        var stats = _entities.GetOrAdd(entry.SourceId, _ => new EntityStatsDictionary());
        stats.Apply(entry);
    }

    public IReadOnlyDictionary<string, EntityStatsDictionary> GetSnapshot()
    {
        // Clone current values
        var snapshot = _entities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Clear internal state
        _entities.Clear();

        return snapshot;
    }
}
