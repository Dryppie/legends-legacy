using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public sealed record SnapshotCombatantRequest(
    CharacterSnapshot Snapshot,
    CombatParticipantSlot Slot);

public interface ISnapshotCombatantBuilder
{
    Task<IReadOnlyList<CombatRuntimeParticipant>> BuildAsync(
        IReadOnlyList<SnapshotCombatantRequest> requests,
        CancellationToken cancellationToken);
}
