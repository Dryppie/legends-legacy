namespace Domain.Models.Dungeons.Runs;

public sealed class DungeonRun
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid? CharacterSnapshotId { get; set; }

    public string DungeonDefinitionId { get; set; } = string.Empty;
    public string DungeonDefinitionName { get; set; } = string.Empty;

    // Deterministic RNG for the run (so you can reproduce and prevent exploits).
    public int Seed { get; set; }

    public DungeonRunStatus Status { get; set; } = DungeonRunStatus.Active;

    public int CurrentRoomIndex { get; set; } = 0;

    // Track what was rolled / selected.
    public List<RoomInstance> Rooms { get; set; } = [];
    public DungeonRunState State { get; set; } = new();


    public int PendingExperience { get; set; }
    public int PendingCinders { get; set; }
    public int PendingSoulstones { get; set; }
    public List<RunReward> PendingRewards { get; set; } = [];
    public int DeathsDuringRun { get; set; }
    public bool UsedRetreat { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? RewardsClaimedAt { get; set; }
    public uint RowVersion { get; set; }
}
