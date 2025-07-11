using Domain.Models.Combat;

namespace Services.LL.Interfaces;
public interface ICombatStatsTracker
{
    void AddLogEntry(CombatLogEntry entry);
    IReadOnlyDictionary<string, EntityStatsDictionary> GetSnapshot();
    IReadOnlyList<EntityStats> Aggregate(IEnumerable<CombatLogItem> log);
}
