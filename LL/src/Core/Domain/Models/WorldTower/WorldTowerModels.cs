using Domain.Models.Snapshots;

namespace Domain.Models.WorldTower;

public enum TowerFloorType
{
    Standard = 0,
    Warden = 1,
    Sovereign = 2
}

public enum TowerFloorStateType
{
    Locked = 0,
    Sealed = 1,
    Scouting = 2,
    Rallying = 3,
    Cleared = 4
}

public enum TowerRallyMode
{
    FirstClear = 0,
    Echo = 1
}

public enum TowerRallyStatus
{
    Recruiting = 0,
    Ready = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TowerRallyApplicationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Withdrawn = 3
}

public enum TowerAttemptStatus
{
    Started = 0,
    Succeeded = 1,
    Failed = 2,
    Errored = 3,
    Playback = 4
}

public enum TowerContributionKind
{
    Research = 0,
    SupplyWeapons = 1,
    InscribeWards = 2,
    ScoutWeakPoints = 3
}

public sealed class TowerFloorProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServerId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public bool IsCleared { get; set; }
    public int ScoutingProgress { get; set; }
    public Guid? FirstClearAttemptId { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
    public DateTimeOffset? ClearedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool Unlock(DateTimeOffset now)
    {
        if (UnlockedAt.HasValue)
            return false;

        UnlockedAt = now;
        UpdatedAt = now;
        return true;
    }

    public void AddScoutingProgress(int amount, DateTimeOffset now)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        ScoutingProgress = IsCleared ? 100 : Math.Min(100, ScoutingProgress + amount);
        UpdatedAt = now;
    }

    public bool RecordFirstClear(Guid attemptId, DateTimeOffset now)
    {
        if (attemptId == Guid.Empty)
            throw new ArgumentException("A first-clear attempt is required.", nameof(attemptId));
        if (IsCleared)
            return false;

        IsCleared = true;
        ScoutingProgress = 100;
        FirstClearAttemptId = attemptId;
        ClearedAt = now;
        UpdatedAt = now;
        return true;
    }
}

public sealed class TowerRally
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServerId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public TowerRallyMode Mode { get; set; }
    public TowerRallyStatus Status { get; set; }
    public Guid CreatedByCharacterId { get; set; }
    public int RequiredSlots { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public ICollection<TowerRallyParticipant> Participants { get; set; } = [];
    public ICollection<TowerRallyApplication> Applications { get; set; } = [];
    public TowerAttempt? Attempt { get; set; }
}

public sealed class TowerRallyApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TowerRallyId { get; set; }
    public TowerRally TowerRally { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public Guid? GuildId { get; set; }
    public string? GuildName { get; set; }
    public int PowerRating { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public CharacterSnapshot CharacterSnapshot { get; set; } = null!;
    public TowerRallyApplicationStatus Status { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByCharacterId { get; set; }
}

public sealed class TowerRallyParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TowerRallyId { get; set; }
    public TowerRally TowerRally { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public Guid? GuildId { get; set; }
    public string? GuildName { get; set; }
    public int PowerRating { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public CharacterSnapshot CharacterSnapshot { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
}

public sealed class TowerAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TowerRallyId { get; set; }
    public TowerRally TowerRally { get; set; } = null!;
    public string ServerId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public TowerRallyMode Mode { get; set; }
    public TowerAttemptStatus Status { get; set; }
    public bool Succeeded { get; set; }
    public int AttemptNumberForFloor { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? FightDurationSeconds { get; set; }
    public string? FailureReason { get; set; }
    public string? CombatResultJson { get; set; }
    public string? BattleReportJson { get; set; }
    public TowerCombatPlayback? Playback { get; set; }
}

public sealed class TowerCombatPlayback
{
    public Guid TowerAttemptId { get; set; }
    public TowerAttempt TowerAttempt { get; set; } = null!;
    public int SchemaVersion { get; set; } = 1;
    public int TicksPerSecond { get; set; } = 10;
    public int TicksPerFrame { get; set; } = 10;
    public int TotalTicks { get; set; }
    public int FrameCount { get; set; }
    public string TimelineJson { get; set; } = "[]";
    public DateTimeOffset SimulationCompletedAt { get; set; }
    public DateTimeOffset PlaybackStartedAt { get; set; }
    public DateTimeOffset PlaybackEndsAt { get; set; }
    public int LastPublishedSequence { get; set; } = -1;
    public long RowVersion { get; set; }
}

public sealed class TowerContribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServerId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public Guid CharacterId { get; set; }
    public TowerContributionKind Kind { get; set; }
    public int Amount { get; set; }
    public int WeekKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TowerEchoClear
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServerId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public Guid CharacterId { get; set; }
    public int WeekKey { get; set; }
    public DateTimeOffset ClearedAt { get; set; }
}

public sealed class ServerUnlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ServerId { get; set; } = string.Empty;
    public string UnlockKey { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "WorldTower";
    public int? SourceFloorNumber { get; set; }
    public DateTimeOffset UnlockedAt { get; set; }
}
