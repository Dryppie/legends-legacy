namespace Domain.Models.Prophecies;

public sealed class PlayerProphecyInstance
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid CharacterId { get; set; }

    public string ProphecyDefinitionId { get; set; } = default!;
    public ProphecyDefinition? ProphecyDefinition { get; set; }

    public ProphecyScope Scope { get; set; }
    public ProphecySlotType SlotType { get; set; }
    public ProphecyStatus Status { get; set; }

    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }

    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }

    public string ObjectiveParameterSnapshotJson { get; set; } = "{}";
    public string ProgressJson { get; set; } = "{}";
    public string RewardSnapshotJson { get; set; } = "{}";

    public uint RowVersion { get; set; }
}
