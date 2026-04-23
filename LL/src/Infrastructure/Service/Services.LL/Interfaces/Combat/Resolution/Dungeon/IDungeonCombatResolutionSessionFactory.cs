using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Resolution.Dungeon;

public interface IDungeonCombatResolutionSessionFactory
{
    Task<ICombatResolutionSession> CreateAsync(
        DungeonCombatPlan plan,
        CancellationToken cancellationToken);
}