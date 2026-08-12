namespace Domain.Models.Combat;

public sealed record CombatCheckpoint(
    int Sequence,
    int Tick,
    IReadOnlyList<SimpleCombatEntity> Friendly,
    IReadOnlyList<SimpleCombatEntity> Hostile,
    IReadOnlyList<EntityStats> EntityStats,
    IReadOnlyList<CombatLogItem> Events,
    bool IsFinal);

public sealed record CombatExecutionWithCheckpoints(
    CombatResult Result,
    IReadOnlyList<CombatCheckpoint> Checkpoints);
