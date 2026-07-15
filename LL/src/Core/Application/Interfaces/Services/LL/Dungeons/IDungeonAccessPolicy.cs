using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonAccessPolicy
{
    Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        int currentCombatRating,
        CancellationToken cancellationToken);

    Task<DungeonAccessResult> EvaluateForSigilAssemblyAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        int currentCombatRating,
        CancellationToken cancellationToken);
}
