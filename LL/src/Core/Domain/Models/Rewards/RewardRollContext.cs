namespace Domain.Models.Rewards;

public sealed record RewardRollContext(
    string Source,
    IReadOnlyDictionary<string, double>? EntryWeightBonusPercentByTag = null,
    IReadOnlyDictionary<string, double>? QuantityBonusPercentByTag = null);
