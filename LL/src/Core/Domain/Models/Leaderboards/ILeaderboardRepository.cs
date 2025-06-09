namespace Domain.Models.Leaderboards;
public interface ILeaderboardRepository
{
    Task<Leaderboard> GetLeaderboardAsync(Guid characterId, CancellationToken cancellationToken);
}
