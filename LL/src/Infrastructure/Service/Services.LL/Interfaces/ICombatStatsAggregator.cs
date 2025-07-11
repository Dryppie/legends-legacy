using Domain.Models.Combat;

namespace Services.LL.Interfaces;
public interface ICombatStatsAggregator
{
    IReadOnlyList<EntityStats> Aggregate(IEnumerable<CombatLogItem> log);
}
