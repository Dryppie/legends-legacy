using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Rewards;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Rewards;

public sealed class RewardRoller : IRewardRoller
{
    private readonly IRewardTableDefinitionProvider _tables;
    private readonly IRandomSource _random;

    public RewardRoller(
        IRewardTableDefinitionProvider tables,
        IRandomSource random)
    {
        _tables = tables;
        _random = random;
    }

    public RewardRollResult Roll(string rewardTableId, RewardRollContext context) =>
        Roll(_tables.GetById(rewardTableId), context);

    public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context)
    {
        var state = new RewardRollState();
        RollTable(table, context, state);

        return new RewardRollResult(
            state.Items,
            state.Cinders,
            state.Soulstones,
            state.Experience,
            state.Trace);
    }

    private void RollTable(
        RewardTableDefinition table,
        RewardRollContext context,
        RewardRollState state)
    {
        foreach (var roll in table.Rolls)
        {
            for (var i = 0; i < Math.Max(1, roll.Rolls); i++)
            {
                if (!PassesChance(roll.Chance))
                {
                    state.Trace.Add(new(table.Id, roll.Id, null, "roll-skipped"));
                    continue;
                }

                ExecuteRoll(table, roll, context, state);
            }
        }
    }

    private void ExecuteRoll(
        RewardTableDefinition table,
        RewardRollDefinition roll,
        RewardRollContext context,
        RewardRollState state)
    {
        switch (roll.Type)
        {
            case RewardRollType.All:
            case RewardRollType.Sequence:
            case RewardRollType.Reference:
                foreach (var entry in roll.Entries)
                {
                    ExecuteEntryIfChancePasses(table.Id, roll.Id, entry, context, state);
                }
                break;

            case RewardRollType.Independent:
                foreach (var entry in roll.Entries)
                {
                    ExecuteEntryIfChancePasses(table.Id, roll.Id, entry, context, state);
                }
                break;

            case RewardRollType.Weighted:
                ExecuteWeighted(table.Id, roll, context, state, includeNoDrop: false);
                break;

            case RewardRollType.WeightedWithNoDrop:
                ExecuteWeighted(table.Id, roll, context, state, includeNoDrop: true);
                break;
        }
    }

    private void ExecuteWeighted(
        string tableId,
        RewardRollDefinition roll,
        RewardRollContext context,
        RewardRollState state,
        bool includeNoDrop)
    {
        var weighted = roll.Entries
            .Select(entry => (Entry: entry, Weight: ApplyEntryWeightBonuses(entry, context)))
            .Where(x => x.Weight > 0)
            .ToList();

        var totalWeight = weighted.Sum(x => x.Weight) + (includeNoDrop ? Math.Max(0, roll.NoDropWeight) : 0);
        if (totalWeight <= 0)
        {
            state.Trace.Add(new(tableId, roll.Id, null, "weighted-empty"));
            return;
        }

        var selected = _random.NextDouble() * totalWeight;
        if (includeNoDrop && selected < roll.NoDropWeight)
        {
            state.Trace.Add(new(tableId, roll.Id, null, "no-drop"));
            return;
        }

        var cursor = includeNoDrop ? Math.Max(0, roll.NoDropWeight) : 0;
        foreach (var (entry, weight) in weighted)
        {
            cursor += weight;
            if (selected > cursor)
                continue;

            ExecuteEntryIfChancePasses(tableId, roll.Id, entry, context, state);
            return;
        }

        state.Trace.Add(new(tableId, roll.Id, null, "weighted-missed"));
    }

    private void ExecuteEntryIfChancePasses(
        string tableId,
        string rollId,
        RewardEntryDefinition entry,
        RewardRollContext context,
        RewardRollState state)
    {
        if (!PassesChance(entry.Chance))
        {
            state.Trace.Add(new(tableId, rollId, entry.Id, "entry-skipped"));
            return;
        }

        ExecuteEntry(tableId, rollId, entry, context, state);
    }

    private void ExecuteEntry(
        string tableId,
        string rollId,
        RewardEntryDefinition entry,
        RewardRollContext context,
        RewardRollState state)
    {
        var quantity = RollQuantity(entry.Quantity);
        quantity = ApplyQuantityBonuses(quantity, entry, context);

        switch (entry.Type)
        {
            case RewardEntryType.Item:
                if (!string.IsNullOrWhiteSpace(entry.ItemId) && quantity > 0)
                {
                    state.Items.Add(new(entry.ItemId, quantity, context.Source));
                    state.Trace.Add(new(tableId, rollId, entry.Id, "item"));
                }
                break;

            case RewardEntryType.Cinders:
                state.Cinders += Math.Max(0, quantity);
                state.Trace.Add(new(tableId, rollId, entry.Id, "cinders"));
                break;

            case RewardEntryType.Soulstones:
                state.Soulstones += Math.Max(0, quantity);
                state.Trace.Add(new(tableId, rollId, entry.Id, "soulstones"));
                break;

            case RewardEntryType.Experience:
                state.Experience += Math.Max(0, quantity);
                state.Trace.Add(new(tableId, rollId, entry.Id, "experience"));
                break;

            case RewardEntryType.RewardTableReference:
                if (!string.IsNullOrWhiteSpace(entry.RewardTableId))
                {
                    state.Trace.Add(new(tableId, rollId, entry.Id, $"reference:{entry.RewardTableId}"));
                    RollTable(_tables.GetById(entry.RewardTableId), context, state);
                }
                break;
        }
    }

    private bool PassesChance(double chance) =>
        chance >= 1 || (chance > 0 && _random.NextDouble() <= chance);

    private double ApplyEntryWeightBonuses(RewardEntryDefinition entry, RewardRollContext context)
    {
        var weight = Math.Max(0, entry.Weight);
        if (context.EntryWeightBonusPercentByTag is null || entry.Tags.Count == 0)
            return weight;

        var bonusPercent = entry.Tags
            .Where(context.EntryWeightBonusPercentByTag.ContainsKey)
            .Sum(tag => Math.Max(0, context.EntryWeightBonusPercentByTag[tag]));

        return weight * (1 + bonusPercent / 100d);
    }

    private int ApplyQuantityBonuses(int quantity, RewardEntryDefinition entry, RewardRollContext context)
    {
        if (quantity <= 0 || context.QuantityBonusPercentByTag is null || entry.Tags.Count == 0)
            return quantity;

        var bonusPercent = entry.Tags
            .Where(context.QuantityBonusPercentByTag.ContainsKey)
            .Sum(tag => Math.Max(0, context.QuantityBonusPercentByTag[tag]));

        return Math.Max(0, (int)Math.Round(quantity * (1 + bonusPercent / 100d)));
    }

    private int RollQuantity(RewardQuantityRange range)
    {
        var min = Math.Max(0, range.Min);
        var max = Math.Max(min, range.Max);
        if (min == max)
            return min;

        return min + (int)Math.Floor(_random.NextDouble() * (max - min + 1));
    }

    private sealed class RewardRollState
    {
        public List<ItemRewardResult> Items { get; } = [];
        public int Cinders { get; set; }
        public int Soulstones { get; set; }
        public int Experience { get; set; }
        public List<RewardRollTrace> Trace { get; } = [];
    }
}
