namespace Application.Interfaces.Services.LL.Guilds;

public interface IGuildMissionService
{
    Task<GuildMissionOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<GuildOperationResult<GuildMissionOverviewDto>> SelectMissionAsync(Guid characterId, Guid missionOptionId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimPersonalOrderRewardAsync(Guid characterId, Guid orderId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimWeeklyRewardAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<GuildContributionResult> RecordContributionAsync(GuildContributionEvent contributionEvent, CancellationToken cancellationToken);
}
