using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;

namespace Services.LL.Dungeons;

public sealed class DungeonAccessPolicy : IDungeonAccessPolicy
{
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IInventoryRepository _inventory;
    private readonly IItemBaseRepository _itemBases;

    public DungeonAccessPolicy(
        IDungeonRunRepository dungeonRuns,
        IInventoryRepository inventory,
        IItemBaseRepository itemBases)
    {
        _dungeonRuns = dungeonRuns;
        _inventory = inventory;
        _itemBases = itemBases;
    }

    public async Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
        => await EvaluateAsync(characterId, dungeon, ignoredEntryCostItemId: null, cancellationToken);

    public async Task<DungeonAccessResult> EvaluateForSigilAssemblyAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        CancellationToken cancellationToken) =>
        await EvaluateAsync(characterId, dungeon, dungeon.SigilItemId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, DungeonPreviewAccess>> EvaluateForPreviewAsync(
        Guid characterId,
        IReadOnlyCollection<DungeonDefinition> dungeons,
        CancellationToken cancellationToken)
    {
        if (dungeons.Count == 0)
        {
            return new Dictionary<string, DungeonPreviewAccess>();
        }

        var itemIds = dungeons
            .SelectMany(x => x.EntryCosts)
            .Where(x => x.Amount > 0 && !string.IsNullOrWhiteSpace(x.ItemId))
            .Select(x => x.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);
        var ownedQuantities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var itemId in itemIds)
        {
            ownedQuantities[itemId] = await _inventory.GetInventoryQuantityAsync(
                characterId,
                itemId,
                cancellationToken);
        }

        var previousDungeonIds = dungeons
            .Select(x => x.RequiredPreviousDungeonId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var completionRecords = await _dungeonRuns.GetCompletionRecordsAsync(
            characterId,
            previousDungeonIds,
            cancellationToken);
        var completedDungeonIds = completionRecords
            .Select(x => x.DungeonDefinitionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return dungeons.ToDictionary(
            dungeon => dungeon.Id,
            dungeon =>
            {
                var requirements = BuildEntryRequirements(dungeon, itemBases, ownedQuantities);
                var completedPrevious = string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
                    || completedDungeonIds.Contains(dungeon.RequiredPreviousDungeonId);
                var entry = BuildAccessResult(
                    requirements,
                    completedPrevious,
                    ignoredEntryCostItemId: null);
                var sigilAssembly = string.IsNullOrWhiteSpace(dungeon.SigilItemId)
                    ? null
                    : BuildAccessResult(
                        requirements,
                        completedPrevious,
                        dungeon.SigilItemId);

                return new DungeonPreviewAccess(entry, sigilAssembly);
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        string? ignoredEntryCostItemId,
        CancellationToken cancellationToken)
    {
        var entryRequirements = await GetEntryRequirementsAsync(
            characterId,
            dungeon,
            cancellationToken);

        var completedPrevious = string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
            || await _dungeonRuns.HasCompletedDungeonAsync(
                characterId,
                dungeon.RequiredPreviousDungeonId,
                cancellationToken);

        return BuildAccessResult(
            entryRequirements,
            completedPrevious,
            ignoredEntryCostItemId);
    }

    private async Task<IReadOnlyList<DungeonEntryRequirementResult>> GetEntryRequirementsAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var costs = dungeon.EntryCosts
            .Where(x => x.Amount > 0 && !string.IsNullOrWhiteSpace(x.ItemId))
            .ToArray();

        if (costs.Length == 0)
        {
            return [];
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            costs.Select(x => x.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            cancellationToken);

        var requirements = new List<DungeonEntryRequirementResult>();

        foreach (var cost in costs)
        {
            var owned = await _inventory.GetInventoryQuantityAsync(
                characterId,
                cost.ItemId,
                cancellationToken);

            var hasItemBase = itemBases.TryGetValue(cost.ItemId, out var itemBase);
            var itemName = hasItemBase ? itemBase!.Name : cost.ItemId;

            requirements.Add(new DungeonEntryRequirementResult(
                cost.ItemId,
                itemName,
                cost.Amount,
                owned,
                hasItemBase ? itemBase!.Description : null));
        }

        return requirements;
    }

    private static IReadOnlyList<DungeonEntryRequirementResult> BuildEntryRequirements(
        DungeonDefinition dungeon,
        IReadOnlyDictionary<string, ItemBase> itemBases,
        IReadOnlyDictionary<string, int> ownedQuantities)
    {
        return dungeon.EntryCosts
            .Where(x => x.Amount > 0 && !string.IsNullOrWhiteSpace(x.ItemId))
            .Select(cost =>
            {
                var hasItemBase = itemBases.TryGetValue(cost.ItemId, out var itemBase);
                return new DungeonEntryRequirementResult(
                    cost.ItemId,
                    hasItemBase ? itemBase!.Name : cost.ItemId,
                    cost.Amount,
                    ownedQuantities.GetValueOrDefault(cost.ItemId),
                    hasItemBase ? itemBase!.Description : null);
            })
            .ToList();
    }

    private static DungeonAccessResult BuildAccessResult(
        IReadOnlyList<DungeonEntryRequirementResult> entryRequirements,
        bool completedPrevious,
        string? ignoredEntryCostItemId)
    {
        var missingRequirements = new List<string>();
        if (!completedPrevious)
        {
            missingRequirements.Add("Complete the previous difficulty first.");
        }

        AddMissingEntryCosts(entryRequirements, missingRequirements, ignoredEntryCostItemId);

        return new DungeonAccessResult(
            missingRequirements.Count == 0,
            missingRequirements,
            entryRequirements);
    }

    private static void AddMissingEntryCosts(
        IReadOnlyList<DungeonEntryRequirementResult> entryRequirements,
        List<string> missingRequirements,
        string? ignoredEntryCostItemId)
    {
        foreach (var requirement in entryRequirements.Where(x =>
                     x.OwnedAmount < x.RequiredAmount &&
                     !x.ItemId.Equals(ignoredEntryCostItemId, StringComparison.OrdinalIgnoreCase)))
        {
            missingRequirements.Add(
                $"Requires {requirement.RequiredAmount} {requirement.Name} ({requirement.OwnedAmount}/{requirement.RequiredAmount}).");
        }
    }

}
