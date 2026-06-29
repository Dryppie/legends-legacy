namespace Domain.Models.Guilds.Missions;

public class GuildMemberContributionPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildId { get; set; }
    public Guid CharacterId { get; set; }
    public GuildMissionPeriodType PeriodType { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public long ContributionScore { get; set; }
    public long GuildFavorEarned { get; set; }
    public long GuildXpGenerated { get; set; }
    public long GuildSuppliesGenerated { get; set; }
    public int OrdersCompleted { get; set; }
    public long WeeklyMissionContribution { get; set; }
    public DateTimeOffset? LastContributedAt { get; set; }
}
