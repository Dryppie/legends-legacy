namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonRun
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    public Guid DungeonDefinitionId { get; set; }

    // Deterministic RNG for the run (so you can reproduce and prevent exploits).
    public int Seed { get; set; }

    public DungeonRunStatus Status { get; set; } = DungeonRunStatus.Active;

    public int CurrentFloorIndex { get; set; } = 0;

    // For combat floors: which encounter within the floor we’re on.
    public int CurrentEncounterIndex { get; set; } = 0;

    // Track what was rolled / selected.
    public List<RunFloorState> Floors { get; set; } = new();

    public List<RunModifier> ActiveModifiers { get; set; } = new(); // run-wide active
    public List<RunBlessing> AppliedBlessings { get; set; } = new();

    public RunFlags Flags { get; set; } = new();

    // Rewards accumulated (banked on withdraw/complete; partial on fail if you choose)
    public List<RunReward> PendingRewards { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}