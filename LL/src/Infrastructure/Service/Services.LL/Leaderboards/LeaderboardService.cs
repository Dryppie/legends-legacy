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

    public async Task<Leaderboard> GetLeaderboardAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _leaderboardRepository.GetLeaderboardAsync(characterId, cancellationToken);
    }
}
