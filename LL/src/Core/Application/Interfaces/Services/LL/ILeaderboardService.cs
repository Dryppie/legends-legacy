using Domain.Models.Leaderboards;

namespace Application.Interfaces.Services.LL;
public interface ILeaderboardService
{
    Task<LeaderboardBoard> GetLeaderboardAsync(
        Guid characterId,
        string boardKey,
        int limit,
        string? cursor,
        string? search,
        CancellationToken cancellationToken);
}
