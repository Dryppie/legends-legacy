using Domain.Models.Leaderboards;

namespace Application.Interfaces.Services.LL;
public interface ILeaderboardService
{
    Task<Leaderboard> GetLeaderboardAsync(Guid characterId, CancellationToken cancellationToken);
}
