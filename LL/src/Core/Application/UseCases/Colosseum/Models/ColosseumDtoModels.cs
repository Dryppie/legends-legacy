using Domain.Models.Colosseum;
using Domain.Models.Combat;

namespace Application.UseCases.Colosseum.Models;

public sealed record StartArenaBattleResponseModel(
    Guid BattleId,
    CombatResult Battle,
    CombatResult Combat,
    ArenaBattleOutcomeModel Outcome,
    ArenaTicketStatus ArenaTicketStatus,
    ArenaRewardModel Rewards,
    ArenaRatingChangeModel AttackerRating,
    ArenaRatingChangeModel DefenderRating,
    ArenaRankChangeModel AttackerRank,
    ArenaStreakChangeModel Streak,
    ArenaOpponentPreview Opponent);

public sealed record StartArenaBattleRequestModel(Guid OpponentId);

public sealed record ArenaBattleOutcomeModel(
    string Result,
    Guid AttackerCharacterId,
    Guid DefenderCharacterId,
    Guid? WinnerCharacterId,
    DateTimeOffset CompletedAt);

public sealed record ArenaRatingChangeModel(
    int RatingBefore,
    int RatingAfter,
    int Delta);

public sealed record ArenaRewardModel(
    int GloryEarned,
    int BaseReward,
    int DailyFirstWinBonus,
    int StreakBonus,
    int DefensiveBonus);

public sealed record ArenaRankChangeModel(
    ArenaRankProgress Before,
    ArenaRankProgress After,
    bool TierChanged);

public sealed record ArenaStreakChangeModel(
    int Before,
    int After,
    int BonusGlory);

public sealed record ColosseumStatusModel(
    int Rating,
    int LifetimeHighestRating,
    ArenaRankProgress RankProgress,
    int Glory,
    int Tickets,
    int MaxTickets,
    DateTimeOffset? NextTicketAt,
    int CurrentAttackWinStreak,
    int BestAttackWinStreak,
    bool DailyFirstWinAvailable,
    int DailyFirstWinBonusGlory,
    ArenaRecordModel AttackRecord,
    ArenaRecordModel DefenseRecord,
    ArenaDefenseStatusModel DefenseStatus);

public sealed record ArenaRecordModel(
    int Wins,
    int Draws,
    int Losses);

public sealed record ArenaDefenseStatusModel(
    bool HasSnapshot,
    bool IsValid,
    bool IsOutdated,
    DateTimeOffset? UpdatedAt,
    string? LoadoutHash);

public sealed record ChampionMarketModel(
    int Glory,
    DateTimeOffset WeeklyResetAt,
    List<ChampionMarketItemModel> Items);

public sealed record ChampionMarketItemModel(
    string Id,
    string Name,
    string Description,
    string Category,
    int GloryCost,
    int? WeeklyPurchaseLimit,
    int? LifetimePurchaseLimit,
    int RemainingWeeklyPurchases,
    int RemainingLifetimePurchases,
    int? RequiredRating,
    string? RequiredRankTier,
    int? RequiredRankMinRating,
    int SortOrder,
    int CindersGranted,
    int SoulstonesGranted,
    int SigilFragmentsGranted,
    string? RewardItemId,
    string? RewardItemName,
    int RewardItemQuantity);

public sealed record PurchaseChampionMarketItemRequestModel(
    string ItemId,
    int Quantity);
