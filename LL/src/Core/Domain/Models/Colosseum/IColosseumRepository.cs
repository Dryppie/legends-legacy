using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public interface IColosseumRepository
{
    Task<(List<Character> Opponents, int MyRating)> GetArenaOpponentsWithRating(Guid characterId, CancellationToken cancellationToken);
    Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken);
    Task<List<Character>> GetRankings(Guid characterId, CancellationToken cancellationToken);
    Task SaveArenaMatchResult(ColosseumMatchResult arenaMatchResult, CancellationToken cancellationToken);
    Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken);
    void UpdateArenaTicketStatus(ArenaTicketStatus arenaTicketStatus);
}