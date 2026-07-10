using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Rewards;

namespace Services.LL.Rewards;

public sealed class RewardTableDefinitionValidator : IRewardTableDefinitionValidator
{
    public IReadOnlyList<string> Validate(
        IReadOnlyList<RewardTableDefinition> definitions,
        IReadOnlySet<string>? itemIds = null)
    {
        var errors = new List<string>();
        var ids = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        errors.AddRange(definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => string.IsNullOrWhiteSpace(x.Key)
                ? "Reward table id is required."
                : $"Duplicate reward table id '{x.Key}'."));

        foreach (var table in definitions)
        {
            ValidateTable(table, ids, itemIds, errors);
        }

        ValidateReferences(definitions, errors);

        return errors;
    }

    public void ThrowIfInvalid(
        IReadOnlyList<RewardTableDefinition> definitions,
        IReadOnlySet<string>? itemIds = null)
    {
        var errors = Validate(definitions, itemIds);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Reward table definition validation failed: " + string.Join(" | ", errors));
        }
    }

    private static void ValidateTable(
        RewardTableDefinition table,
        IReadOnlySet<string> tableIds,
        IReadOnlySet<string>? itemIds,
        List<string> errors)
    {
        var tableId = string.IsNullOrWhiteSpace(table.Id) ? "<missing id>" : table.Id;

        if (table.Rolls.Count == 0)
        {
            errors.Add($"{tableId}: at least one roll is required.");
            return;
        }

        errors.AddRange(table.Rolls
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => string.IsNullOrWhiteSpace(x.Key)
                ? $"{tableId}: reward roll id is required."
                : $"{tableId}: duplicate reward roll id '{x.Key}'."));

        foreach (var roll in table.Rolls)
        {
            ValidateRoll(tableId, roll, tableIds, itemIds, errors);
        }
    }

    private static void ValidateRoll(
        string tableId,
        RewardRollDefinition roll,
        IReadOnlySet<string> tableIds,
        IReadOnlySet<string>? itemIds,
        List<string> errors)
    {
        var rollId = string.IsNullOrWhiteSpace(roll.Id) ? "<missing id>" : roll.Id;

        if (roll.Rolls <= 0)
            errors.Add($"{tableId}.{rollId}: rolls must be greater than zero.");

        if (roll.Chance is < 0 or > 1)
            errors.Add($"{tableId}.{rollId}: chance must be between 0 and 1.");

        if (roll.NoDropWeight < 0)
            errors.Add($"{tableId}.{rollId}: noDropWeight cannot be negative.");

        if (roll.Entries.Count == 0)
            errors.Add($"{tableId}.{rollId}: at least one entry is required.");

        if (roll.Type is RewardRollType.Weighted or RewardRollType.WeightedWithNoDrop)
        {
            var totalWeight = roll.Entries.Sum(x => Math.Max(0, x.Weight));
            if (totalWeight <= 0)
                errors.Add($"{tableId}.{rollId}: weighted rolls require positive entry weight.");
        }

        foreach (var entry in roll.Entries)
        {
            ValidateEntry(tableId, rollId, roll.Type, entry, tableIds, itemIds, errors);
        }
    }

    private static void ValidateEntry(
        string tableId,
        string rollId,
        RewardRollType rollType,
        RewardEntryDefinition entry,
        IReadOnlySet<string> tableIds,
        IReadOnlySet<string>? itemIds,
        List<string> errors)
    {
        var entryId = string.IsNullOrWhiteSpace(entry.Id) ? "<missing id>" : entry.Id;

        if (string.IsNullOrWhiteSpace(entry.Id))
            errors.Add($"{tableId}.{rollId}: entry id is required.");

        if (entry.Chance is < 0 or > 1)
            errors.Add($"{tableId}.{rollId}.{entryId}: chance must be between 0 and 1.");

        if (rollType is RewardRollType.Weighted or RewardRollType.WeightedWithNoDrop)
        {
            if (entry.Weight <= 0)
                errors.Add($"{tableId}.{rollId}.{entryId}: weighted entries require positive weight.");
        }

        if (entry.Quantity.Min < 0)
            errors.Add($"{tableId}.{rollId}.{entryId}: quantity min cannot be negative.");

        if (entry.Quantity.Max < entry.Quantity.Min)
            errors.Add($"{tableId}.{rollId}.{entryId}: quantity max must be greater than or equal to min.");

        if (RequiresPositiveQuantity(entry.Type) && entry.Quantity.Max <= 0)
            errors.Add($"{tableId}.{rollId}.{entryId}: quantity max must be greater than zero.");

        if (entry.Type == RewardEntryType.Item)
        {
            if (string.IsNullOrWhiteSpace(entry.ItemId))
            {
                errors.Add($"{tableId}.{rollId}.{entryId}: item entries require itemId.");
            }
            else if (itemIds is not null && !itemIds.Contains(entry.ItemId))
            {
                errors.Add($"{tableId}.{rollId}.{entryId}: item '{entry.ItemId}' does not exist.");
            }
        }

        if (entry.Type == RewardEntryType.RewardTableReference)
        {
            if (string.IsNullOrWhiteSpace(entry.RewardTableId))
            {
                errors.Add($"{tableId}.{rollId}.{entryId}: reward table references require rewardTableId.");
            }
            else if (!tableIds.Contains(entry.RewardTableId))
            {
                errors.Add($"{tableId}.{rollId}.{entryId}: referenced reward table '{entry.RewardTableId}' does not exist.");
            }
        }
    }

    private static bool RequiresPositiveQuantity(RewardEntryType type) =>
        type is RewardEntryType.Item or RewardEntryType.Cinders or RewardEntryType.Soulstones or RewardEntryType.Experience;

    private static void ValidateReferences(IReadOnlyList<RewardTableDefinition> definitions, List<string> errors)
    {
        var edges = definitions.ToDictionary(
            x => x.Id,
            x => x.Rolls
                .SelectMany(roll => roll.Entries)
                .Where(entry => entry.Type == RewardEntryType.RewardTableReference && !string.IsNullOrWhiteSpace(entry.RewardTableId))
                .Select(entry => entry.RewardTableId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var tableId in edges.Keys)
        {
            DetectCycles(tableId, edges, [], [], errors);
        }
    }

    private static void DetectCycles(
        string tableId,
        IReadOnlyDictionary<string, List<string>> edges,
        HashSet<string> visiting,
        HashSet<string> visited,
        List<string> errors)
    {
        if (visited.Contains(tableId))
            return;

        if (!visiting.Add(tableId))
        {
            errors.Add($"Reward table reference cycle detected at '{tableId}'.");
            return;
        }

        if (edges.TryGetValue(tableId, out var next))
        {
            foreach (var child in next)
            {
                DetectCycles(child, edges, visiting, visited, errors);
            }
        }

        visiting.Remove(tableId);
        visited.Add(tableId);
    }
}
