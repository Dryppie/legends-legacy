using Domain.Models.Combat;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed record CombatEncounterResolutionResult(
    Guid EncounterId,
    CombatMode Mode,
    int Sequence,
    DateTimeOffset StartedAt,
    BattleOutcome Outcome,
    CombatResult CombatResult,
    IReadOnlyList<SimpleCombatEntity> FriendlyPostState,
    IReadOnlyList<SimpleCombatEntity> HostilePostState);