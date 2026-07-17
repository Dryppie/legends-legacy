namespace Domain.Models.Leaderboards;
public interface ILeaderboardRepository
{
    Task<LeaderboardBoard> GetLeaderboardAsync(
        Guid characterId,
        string boardKey,
        int limit,
        string? cursor,
        string? search,
        CancellationToken cancellationToken);
}
