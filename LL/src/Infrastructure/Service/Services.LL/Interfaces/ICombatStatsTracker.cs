using Domain.Models.Combat;

namespace Services.LL.Interfaces;
public interface ICombatStatsTracker
{
    void AddLogEntry(CombatLogEntry entry);
    IReadOnlyDictionary<string, EntityStatsDictionary> GetSnapshot();
}
