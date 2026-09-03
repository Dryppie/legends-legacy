using Application.Interfaces.Services.LL;
using Domain.Models.Leaderboards;

namespace Services.LL.Leaderboards;

public sealed class LeaderboardService(ILeaderboardRepository repository) : ILeaderboardService
{
    public Task<LeaderboardBoard> GetLeaderboardAsync(Guid characterId, string boardKey, int limit,
        string? cursor, string? search, CancellationToken cancellationToken) =>
        repository.GetLeaderboardAsync(characterId, boardKey, Math.Clamp(limit, 10, 100), cursor, search?.Trim(), cancellationToken);
}
