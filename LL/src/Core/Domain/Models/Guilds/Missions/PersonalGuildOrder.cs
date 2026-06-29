namespace Domain.Models.Guilds.Missions;

public class PersonalGuildOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid MissionDefinitionId { get; set; }
    public GuildMissionPeriodType PeriodType { get; set; } = GuildMissionPeriodType.Daily;
    public string PeriodKey { get; set; } = string.Empty;
    public long TargetAmount { get; set; }
    public long CurrentAmount { get; set; }
    public PersonalGuildOrderStatus Status { get; set; } = PersonalGuildOrderStatus.Active;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? RewardClaimedAt { get; set; }
}
