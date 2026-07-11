namespace Domain.Models.Rewards;

public sealed record RewardRollResult(
    IReadOnlyList<ItemRewardResult> Items,
    int Cinders,
    int Soulstones,
    int Experience,
    IReadOnlyList<RewardRollTrace> Trace)
{
    public static RewardRollResult Empty { get; } = new([], 0, 0, 0, []);
}
