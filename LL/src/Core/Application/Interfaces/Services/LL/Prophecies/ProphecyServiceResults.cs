using Domain.Models.Prophecies;

namespace Application.Interfaces.Services.LL.Prophecies;

public sealed record PropheciesOverview(
    DateTimeOffset ServerTime,
    int DailyRerollsRemaining,
    int DailyRerollsUsed,
    int DailyRerollLimit,
    int? NextDailyRerollCost,
    long FateEcho,
    IReadOnlyList<PlayerProphecyInstance> DailyProphecies,
    PlayerProphecyInstance? ActiveDailyProphecy,
    PlayerProphecyInstance GreaterProphecy,
    WeeklyRevelationProgress WeeklyRevelation,
    IReadOnlyList<WeeklyRevelationMilestone> WeeklyMilestones,
    IReadOnlyList<ProphecyCacheInventory> Caches);

public sealed record WeeklyRevelationMilestone(
    int FavorRequired,
    string Title,
    bool IsUnlocked,
    bool IsClaimed,
    ProphecyRewardSnapshot Reward);

public sealed record ProphecyCacheInventory(
    string ItemId,
    string Title,
    string Description,
    int Quantity,
    IReadOnlyList<string> PossibleRewards);

public sealed record ProphecyOperationResult<T>(bool Succeeded, string? Error, T? Value)
{
    public static ProphecyOperationResult<T> Success(T value) => new(true, null, value);
    public static ProphecyOperationResult<T> Fail(string error) => new(false, error, default);
}

public sealed record ProphecyClaimResult(
    PlayerProphecyInstance Prophecy,
    ProphecyRewardSnapshot Reward,
    WeeklyRevelationProgress WeeklyRevelation,
    IReadOnlyList<WeeklyRevelationMilestone> WeeklyMilestones);

public sealed record WeeklyRevelationClaimResult(
    int FavorRequired,
    ProphecyRewardSnapshot Reward,
    WeeklyRevelationProgress WeeklyRevelation,
    IReadOnlyList<WeeklyRevelationMilestone> WeeklyMilestones);

public sealed record ProphecyCacheOpenResult(
    string CacheItemId,
    ProphecyRewardSnapshot Reward,
    IReadOnlyList<ProphecyCacheInventory> Caches);

public sealed record ProphecyProgressUpdate(
    Guid CharacterId,
    Guid ProphecyId,
    string Title,
    string Scope,
    string SlotType,
    string Status,
    int CurrentValue,
    int TargetValue,
    int AmountGained,
    bool Completed);
