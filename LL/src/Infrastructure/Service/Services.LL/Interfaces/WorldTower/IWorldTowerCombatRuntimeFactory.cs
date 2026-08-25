using Domain.Models.Entities.Creatures;
using Domain.Models.WorldTower;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Interfaces.WorldTower;

public sealed record WorldTowerCombatRuntimeRequest(
    Guid EncounterId,
    Guid RallyId,
    TowerFloorDefinition Definition,
    IReadOnlyList<SnapshotCombatantRequest> FriendlyCombatants,
    Creature GuardianSource,
    decimal PlayerDamagePercent,
    decimal WeakPointPercent,
    decimal GuardianDamageReductionPercent,
    DateTimeOffset StartsAt);

public interface IWorldTowerCombatRuntimeFactory
{
    Task<CombatEncounterRuntime> CreateAsync(
        WorldTowerCombatRuntimeRequest request,
        CancellationToken cancellationToken);
}
