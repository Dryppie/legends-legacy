using Domain.Models.Colosseum;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Leaderboards;

namespace Application.Interfaces.Services.LL.Colosseum;

public sealed record StartArenaBattleResult(
    Guid BattleId,
    CombatResult CombatResult,
    ArenaTicketStatus ArenaTicketStatus,
    ColosseumMatchResult MatchResult,
    ArenaRankProgress AttackerRankBefore,
    ArenaRankProgress AttackerRankAfter,
    ArenaOpponentPreview Opponent,
    int GloryEarned,
    int BaseGloryEarned,
    int DailyFirstWinBonus,
    int DefenderGloryEarned,
    int AttackStreakBefore,
    int AttackStreakAfter);

public sealed record ChampionMarketPurchaseResult(
    ChampionMarketItem Item,
    int Quantity,
    int GlorySpent,
    int GloryRemaining,
    int CindersGranted,
    int SoulstonesGranted,
    int SigilFragmentsGranted,
    string? RewardItemId,
    string? RewardItemName,
    int RewardItemQuantity,
    IReadOnlyList<InventoryItem> InventoryItemsGranted);

public sealed record ChampionMarketTitleGrant(
    Guid CharacterId,
    string ItemId,
    string TitleKey,
    DateTimeOffset PurchasedAt);

public interface IColosseumService
{
    Task<IReadOnlyList<ArenaOpponentPreview>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken);
    Task<ArenaTicketStatus> GetArenaTicketStatusAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Character?> GetArenaCharacterAsync(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Get a previous match results from the arena
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<ColosseumMatchResult>> GetColosseumMatchResults(Guid characterId, CancellationToken cancellationToken);
    Task<List<LeaderboardEntry>> GetRankings(Guid characterId, CancellationToken cancellationToken);

    /// <summary>
    /// Method to handle the event of saving an arena match after it's finished
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="enemyId"></param>
    /// <param name="outcome"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveArenaMatchResult(Guid characterId, Guid enemyId, BattleOutcome outcome, ColosseumRatingResult ratingResult, CancellationToken cancellationToken);
    Task<StartArenaBattleResult?> StartArenaBattle(Guid characterId, Guid enemyId, CancellationToken cancellationToken);
    Task<ArenaDefenseSnapshot?> UpdateDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken);
    Task<ArenaDefenseSnapshot?> GetArenaDefenseSnapshotAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChampionMarketItem>> GetChampionMarketItemsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<ChampionMarketPurchaseResult?> PurchaseChampionMarketItemAsync(Guid characterId, string itemId, int quantity, CancellationToken cancellationToken);
    Task<int> CountChampionMarketPurchasesAsync(Guid characterId, string itemId, DateTimeOffset? since, CancellationToken cancellationToken);

    /// <summary>
    /// Grants Champion's Market title rewards for past purchases that never produced a title unlock.
    /// Title rewards were charged but not granted before the reward pipeline existed, so this repairs
    /// those purchases. Safe to run repeatedly: already-unlocked titles are skipped.
    /// </summary>
    Task<IReadOnlyList<ChampionMarketTitleGrant>> BackfillMissingChampionMarketTitleGrantsAsync(
        CancellationToken cancellationToken);
}
