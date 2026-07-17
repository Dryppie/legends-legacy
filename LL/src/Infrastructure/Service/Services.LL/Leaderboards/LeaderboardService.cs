using Application.Interfaces.Services.LL;
using Domain.Models.Leaderboards;

namespace Services.LL.Leaderboards;
public class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _leaderboardRepository;

    public LeaderboardService(ILeaderboardRepository leaderboardRepository)
    {
        _leaderboardRepository = leaderboardRepository;
    }

    public async Task<LeaderboardBoard> GetLeaderboardAsync(
        Guid characterId,
        string boardKey,
        int limit,
        string? cursor,
        string? search,
        CancellationToken cancellationToken)
    {
        return await _leaderboardRepository.GetLeaderboardAsync(
            characterId,
            boardKey,
            Math.Clamp(limit, 10, 100),
            cursor,
            search?.Trim(),
            cancellationToken);
    }
}
