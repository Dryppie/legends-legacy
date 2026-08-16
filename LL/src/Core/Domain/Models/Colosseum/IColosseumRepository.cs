using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public interface IColosseumRepository
{
    Task<Character?> GetArenaCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<(List<Character> Opponents, int MyRating)> GetArenaOpponentsWithRating(Guid characterId, CancellationToken cancellationToken);
    Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasRecentMatchAsync(Guid attackerCharacterId, Guid defenderCharacterId, DateTimeOffset since, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetRecentAttackerMatchTimesAsync(
        Guid attackerCharacterId,
        IReadOnlyCollection<Guid> defenderCharacterIds,
        DateTimeOffset since,
        CancellationToken cancellationToken);
    Task<List<Character>> GetRankings(Guid characterId, CancellationToken cancellationToken);
    Task SaveArenaMatchResult(ColosseumMatchResult arenaMatchResult, CancellationToken cancellationToken);
    Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken);
    void UpdateArenaTicketStatus(ArenaTicketStatus arenaTicketStatus);
    Task<ArenaDefenseSnapshot?> GetArenaDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken);
    Task SaveArenaDefenseSnapshotAsync(ArenaDefenseSnapshot snapshot, CancellationToken cancellationToken);
    Task<int> CountChampionMarketPurchasesAsync(Guid characterId, string itemId, DateTimeOffset? since, CancellationToken cancellationToken);
    Task SaveChampionMarketPurchaseAsync(ChampionMarketPurchase purchase, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChampionMarketPurchase>> GetChampionMarketPurchasesByItemIdsAsync(
        IReadOnlyCollection<string> itemIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, Guid>> GetAccountIdsForCharactersAsync(
        IReadOnlyCollection<Guid> characterIds,
        CancellationToken cancellationToken);
}
