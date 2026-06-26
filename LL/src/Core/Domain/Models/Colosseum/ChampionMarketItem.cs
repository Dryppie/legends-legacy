namespace Domain.Models.Colosseum;

public sealed record ChampionMarketItem(
    string Id,
    string Name,
    string Description,
    string Category,
    int GloryCost,
    int? WeeklyPurchaseLimit,
    int? LifetimePurchaseLimit,
    int? RequiredRating,
    string? RequiredRankTier,
    bool IsEnabled,
    int SortOrder,
    int CindersGranted = 0,
    int SoulstonesGranted = 0);
