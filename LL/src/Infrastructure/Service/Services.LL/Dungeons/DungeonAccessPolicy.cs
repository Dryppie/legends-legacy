using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace Services.LL.Dungeons;

public sealed class DungeonAccessPolicy : IDungeonAccessPolicy
{
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IInventoryRepository _inventory;
    private readonly IItemBaseRepository _itemBases;
    private readonly IDbContext _db;
    private readonly string _serverId;

    public DungeonAccessPolicy(
        IDungeonRunRepository dungeonRuns,
        IInventoryRepository inventory,
        IItemBaseRepository itemBases,
        IDbContext db,
        IOptions<WorldTowerOptions> towerOptions)
    {
        _dungeonRuns = dungeonRuns;
        _inventory = inventory;
        _itemBases = itemBases;
        _db = db;
        _serverId = towerOptions.Value.ServerId;
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
        CancellationToken cancellationToken) =>
        await EvaluateForPreviewAsync(
            characterId,
            dungeons,
            new Dictionary<string, int>(),
            cancellationToken);

    public async Task<IReadOnlyDictionary<string, DungeonPreviewAccess>> EvaluateForPreviewAsync(
        Guid characterId,
        IReadOnlyCollection<DungeonDefinition> dungeons,
        IReadOnlyDictionary<string, int> inventoryQuantityOverrides,
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
        var normalizedQuantityOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (itemId, quantity) in inventoryQuantityOverrides)
        {
            normalizedQuantityOverrides[itemId] = Math.Max(0, quantity);
        }

        foreach (var itemId in itemIds)
        {
            ownedQuantities[itemId] = normalizedQuantityOverrides.TryGetValue(itemId, out var quantityOverride)
                ? quantityOverride
                : await _inventory.GetInventoryQuantityAsync(
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
        var clearedTowerFloors = await GetClearedTowerFloorsAsync(dungeons, cancellationToken);

        var result = dungeons.ToDictionary(
            dungeon => dungeon.Id,
            dungeon =>
            {
                var requirements = BuildEntryRequirements(dungeon, itemBases, ownedQuantities);
                var completedPrevious = string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
                    || completedDungeonIds.Contains(dungeon.RequiredPreviousDungeonId);
                var towerFloorRequirementMet = !dungeon.RequiredTowerFloor.HasValue
                    || clearedTowerFloors.Contains(dungeon.RequiredTowerFloor.Value);
                var entry = BuildAccessResult(
                    requirements,
                    completedPrevious,
                    towerFloorRequirementMet,
                    dungeon.RequiredTowerFloor,
                    ignoredEntryCostItemId: null);
                var sigilAssembly = string.IsNullOrWhiteSpace(dungeon.SigilItemId)
                    ? null
                    : BuildAccessResult(
                        requirements,
                        completedPrevious,
                        towerFloorRequirementMet,
                        dungeon.RequiredTowerFloor,
                        dungeon.SigilItemId);

                return new DungeonPreviewAccess(entry, sigilAssembly);
            },
            StringComparer.OrdinalIgnoreCase);
        return result;
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
        var towerFloorRequirementMet = await HasClearedRequiredTowerFloorAsync(
            dungeon.RequiredTowerFloor,
            cancellationToken);

        return BuildAccessResult(
            entryRequirements,
            completedPrevious,
            towerFloorRequirementMet,
            dungeon.RequiredTowerFloor,
            ignoredEntryCostItemId);
    }

    private async Task<bool> HasClearedRequiredTowerFloorAsync(
        int? requiredTowerFloor,
        CancellationToken cancellationToken)
    {
        if (!requiredTowerFloor.HasValue)
        {
            return true;
        }

        return await _db.TowerFloorProgresses
            .AsNoTracking()
            .AnyAsync(
                progress =>
                    progress.ServerId == _serverId
                    && progress.IsCleared
                    && progress.FloorNumber >= requiredTowerFloor.Value,
                cancellationToken);
    }

    private async Task<HashSet<int>> GetClearedTowerFloorsAsync(
        IReadOnlyCollection<DungeonDefinition> dungeons,
        CancellationToken cancellationToken)
    {
        var requiredFloors = dungeons
            .Where(dungeon => dungeon.RequiredTowerFloor.HasValue)
            .Select(dungeon => dungeon.RequiredTowerFloor!.Value)
            .Distinct()
            .ToArray();
        if (requiredFloors.Length == 0)
        {
            return [];
        }

        var highestClearedFloor = await _db.TowerFloorProgresses
            .AsNoTracking()
            .Where(progress =>
                progress.ServerId == _serverId
                && progress.IsCleared)
            .Select(progress => (int?)progress.FloorNumber)
            .MaxAsync(cancellationToken);

        if (!highestClearedFloor.HasValue)
        {
            return [];
        }

        return requiredFloors
            .Where(requiredFloor => requiredFloor <= highestClearedFloor.Value)
            .ToHashSet();
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
        bool towerFloorRequirementMet,
        int? requiredTowerFloor,
        string? ignoredEntryCostItemId)
    {
        var missingRequirements = new List<string>();
        if (!completedPrevious)
        {
            missingRequirements.Add("Complete the previous difficulty first.");
        }

        if (!towerFloorRequirementMet && requiredTowerFloor.HasValue)
        {
            missingRequirements.Add($"Requires World Tower Floor {requiredTowerFloor.Value} to be completed.");
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
