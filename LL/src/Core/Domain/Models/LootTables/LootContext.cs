using Domain.Models.Items;

namespace Domain.Models.LootTables;
public sealed record LootContext
{
    public LootSource Source { get; init; }
    public IDictionary<ItemType, double> TypeMultipliers { get; init; } = new Dictionary<ItemType, double>();
    public double RareEntryWeightBonusPercent { get; init; }
}
