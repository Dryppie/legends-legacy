using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonAccessPolicy
{
    Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        CancellationToken cancellationToken);

    Task<DungeonAccessResult> EvaluateForSigilAssemblyAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, DungeonPreviewAccess>> EvaluateForPreviewAsync(
        Guid characterId,
        IReadOnlyCollection<DungeonDefinition> dungeons,
        CancellationToken cancellationToken);
}
