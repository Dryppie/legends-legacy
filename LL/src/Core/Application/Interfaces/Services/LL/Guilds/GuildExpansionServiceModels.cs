using Domain.Models.Guilds;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Shop;
using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Guilds;

public sealed record GuildOperationResult<T>(bool Succeeded, string? Error, T? Value)
{
    public static GuildOperationResult<T> Success(T value) => new(true, null, value);
    public static GuildOperationResult<T> Fail(string error) => new(false, error, default);
}

public sealed record GuildContributionEvent(
    Guid CharacterId,
    GuildContributionSource Source,
    GuildContributionMetric Metric,
    long Amount,
    Guid? AccountId = null,
    string? ContextId = null,
    IReadOnlyDictionary<string, string>? Tags = null,
    DateTimeOffset? OccurredAt = null,
    string? IdempotencyKey = null);

public sealed record GuildContributionResult(
    bool Succeeded,
    bool WasDuplicate,
    long WeeklyProgressAdded,
    int PersonalOrdersCompleted);

public sealed record GuildMissionDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    GuildMissionCategory Category,
    GuildContributionMetric Metric,
    long BaseTarget);

public sealed record GuildMissionOptionDto(
    Guid Id,
    GuildMissionDefinitionDto Definition,
    string WeekKey,
    DateTimeOffset ExpiresAt,
    bool IsSelected);

public sealed record GuildMissionInstanceDto(
    Guid Id,
    GuildMissionDefinitionDto Definition,
    string WeekKey,
    long TargetAmount,
    long CurrentAmount,
    GuildMissionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset EndsAt,
    DateTimeOffset RewardClaimDeadline,
    IReadOnlyList<GuildWeeklyRewardTierDto> RewardTiers);

public sealed record GuildMissionRewardDto(
    long GuildFavor,
    long GuildXp,
    int GuildSupplies);

public sealed record GuildWeeklyRewardTierDto(
    GuildContributionTier Tier,
    long RequiredContribution,
    GuildMissionRewardDto Reward);

public sealed record GuildMissionContributionDto(
    long Amount,
    GuildContributionTier Tier,
    DateTimeOffset? LastContributedAt,
    bool RewardClaimed,
    bool CanClaimReward);

public sealed record PersonalGuildOrderDto(
    Guid Id,
    GuildMissionDefinitionDto Definition,
    string PeriodKey,
    long TargetAmount,
    long CurrentAmount,
    PersonalGuildOrderStatus Status,
    bool CanClaimReward,
    GuildMissionRewardDto Reward,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? CompletedAt);

public sealed record GuildContributionSummaryDto(
    string DailyPeriodKey,
    string WeeklyPeriodKey,
    long DailyContributionScore,
    long WeeklyContributionScore,
    long GuildFavorEarned,
    long GuildXpGenerated,
    long GuildSuppliesGenerated,
    int OrdersCompleted);

public sealed record GuildContributionLeaderboardEntryDto(
    Guid CharacterId,
    string CharacterName,
    long WeeklyContributionScore,
    long WeeklyMissionContribution,
    long GuildFavorEarned,
    long GuildXpGenerated,
    long GuildSuppliesGenerated,
    int OrdersCompleted,
    DateTimeOffset? LastContributedAt);

public sealed record GuildMissionOverviewDto(
    Guid GuildId,
    long GuildXp,
    int GuildLevel,
    DateTimeOffset NextDailyResetAt,
    DateTimeOffset NextWeeklyResetAt,
    bool CanSelectMission,
    IReadOnlyList<GuildMissionOptionDto> WeeklyOptions,
    GuildMissionInstanceDto? ActiveMission,
    GuildMissionContributionDto? MyWeeklyContribution,
    IReadOnlyList<PersonalGuildOrderDto> PersonalOrders,
    GuildContributionSummaryDto ContributionSummary,
    IReadOnlyList<GuildContributionLeaderboardEntryDto> ContributionLeaderboard);

public sealed record GuildShopRewardDto(
    GuildShopRewardType Type,
    long Amount,
    string? Key = null,
    string? Name = null,
    string? Description = null);

public sealed record GuildShopItemDto(
    string Key,
    string Name,
    string Description,
    GuildShopStockType StockType,
    long GuildFavorCost,
    int WeeklyLimit,
    int PurchasedThisPeriod,
    int RequiredMarketOfficeLevel,
    bool IsInWeeklyRotation,
    IReadOnlyList<GuildShopRewardDto> Rewards,
    bool CanPurchase,
    string? LockedReason);

public sealed record GuildShopOverviewDto(
    Guid GuildId,
    long GuildFavor,
    string WeeklyPeriodKey,
    DateTimeOffset NextWeeklyResetAt,
    IReadOnlyList<GuildShopItemDto> Items);

public sealed record GuildShopPurchaseResult(
    GuildShopOverviewDto Shop,
    IReadOnlyList<InventoryItem> InventoryItemsGranted);

public sealed record GuildBuildingBenefitDto(
    int Level,
    string Title,
    string Description,
    bool IsImplemented);

public sealed record GuildBuildingDefinitionDto(
    GuildBuildingType Type,
    string Name,
    string Description,
    int MaxLevel,
    bool IsPermanent,
    int RequiredGuildHallLevel,
    string UnlockSummary,
    IReadOnlyList<GuildBuildingBenefitDto> Benefits);

public sealed record GuildBuildingDto(
    Guid? Id,
    GuildBuildingDefinitionDto Definition,
    int Level,
    IReadOnlyDictionary<GuildResourceType, int>? NextCost,
    bool CanConstruct,
    bool CanUpgrade,
    string? LockedReason);

public sealed record GuildActivityLogDto(
    GuildActivityLogType Type,
    Guid? CharacterId,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record GuildBuildingTargetDto(
    GuildBuildingType Type,
    string Name,
    int TargetLevel);

public sealed record GuildBuildingOverviewDto(
    Guid GuildId,
    int GuildHallLevel,
    long GuildSupplies,
    bool CanManageBuildings,
    GuildBuildingTargetDto? CurrentTarget,
    IReadOnlyList<GuildBuildingDto> Buildings,
    IReadOnlyList<GuildActivityLogDto> ActivityLogs);
