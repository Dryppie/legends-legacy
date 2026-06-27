namespace Domain.Models.Guilds.Missions;

public class GuildMissionInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid MissionDefinitionId { get; set; }
    public string WeekKey { get; set; } = string.Empty;
    public long TargetAmount { get; set; }
    public long CurrentAmount { get; set; }
    public GuildMissionStatus Status { get; set; } = GuildMissionStatus.PendingSelection;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset RewardClaimDeadline { get; set; }
    public ICollection<GuildMissionContribution> Contributions { get; set; } = [];
}
