namespace Domain.Models.Combat;

public sealed record CombatCheckpoint(
    int Sequence,
    int Tick,
    IReadOnlyList<SimpleCombatEntity> Friendly,
    IReadOnlyList<SimpleCombatEntity> Hostile,
    IReadOnlyList<EntityStats> EntityStats,
    IReadOnlyList<CombatLogItem> Events,
    bool IsFinal,
    CombatCheckpointContext? Context = null);

public sealed record CombatCheckpointContext(
    int WaveNumber,
    int FuryStacks,
    IReadOnlyList<CombatDownedState> Downed);

public sealed record CombatDownedState(
    string EntityId,
    int Deaths,
    int ReviveAtTick,
    int RemainingTicks);

public sealed record CombatExecutionWithCheckpoints(
    CombatResult Result,
    IReadOnlyList<CombatCheckpoint> Checkpoints);
