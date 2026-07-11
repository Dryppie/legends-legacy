namespace Domain.Models.Rewards;

public sealed record ItemRewardResult(
    string ItemId,
    int Quantity,
    string Source);
