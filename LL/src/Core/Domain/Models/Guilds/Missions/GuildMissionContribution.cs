namespace Domain.Models.Guilds.Missions;

public class GuildMissionContribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuildMissionInstanceId { get; set; }
    public GuildMissionInstance GuildMissionInstance { get; set; } = null!;
    public Guid GuildId { get; set; }
    public Guid CharacterId { get; set; }
    public long Amount { get; set; }
    public GuildContributionTier ContributionTier { get; set; } = GuildContributionTier.None;
    public DateTimeOffset? LastContributedAt { get; set; }
    public DateTimeOffset? RewardClaimedAt { get; set; }
}
