namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonRun
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    public string DungeonDefinitionId { get; set; } = string.Empty;
    public string DungeonDefinitionName { get; set; } = string.Empty;

    // Deterministic RNG for the run (so you can reproduce and prevent exploits).
    public int Seed { get; set; }

    public DungeonRunStatus Status { get; set; } = DungeonRunStatus.Active;

    public int CurrentRoomIndex { get; set; } = 0;

    // Track what was rolled / selected.
    public List<RoomInstance> Rooms { get; set; } = [];

    //public List<RunModifier> ActiveModifiers { get; set; } = []; // run-wide active
    //public List<RunBlessing> AppliedBlessings { get; set; } = [];

    //public RunFlags Flags { get; set; } = new();

    // Rewards accumulated (banked on withdraw/complete; partial on fail if you choose)
    //public List<RunReward> PendingRewards { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}