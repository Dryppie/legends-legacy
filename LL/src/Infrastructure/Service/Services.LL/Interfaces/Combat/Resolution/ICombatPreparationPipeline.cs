using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public abstract record CombatantPreparationSource;

public sealed record LiveCombatantPreparationSource(
    Entity Entity,
    Area? CreatureArea = null) : CombatantPreparationSource;

public sealed record SnapshotCombatantPreparationSource(
    CharacterSnapshot Snapshot) : CombatantPreparationSource;

public sealed record CombatantPreparationRequest(
    CombatParticipantSlot Slot,
    CombatantPreparationSource Source,
    Action<CombatEntity>? ConfigureBeforePreparation = null,
    Action<CombatEntity>? ConfigureAfterPreparation = null);

public interface ICombatPreparationPipeline
{
    Task<IReadOnlyList<CombatRuntimeParticipant>> PrepareAsync(
        CombatContentType contentType,
        IReadOnlyList<CombatantPreparationRequest> requests,
        CancellationToken cancellationToken);
}
