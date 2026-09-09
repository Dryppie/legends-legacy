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
    IReadOnlyList<SimpleCombatEntity> HostilePostState)
{
    public required CombatContentType ContentType { get; init; }
    public IReadOnlyList<CombatStyleExperienceAward> CombatStyleExperience { get; init; } = [];
    public BattleOutcome EngineOutcome => CombatResult.EngineOutcome;
    public BattleOutcome ContentOutcome => CombatResult.ContentOutcome;
}

public sealed record CombatStyleExperienceAward(Guid CharacterId, string CapturedCombatStyleId,
    long EligibleBaseExperience, long ExperienceGranted, int Level);
