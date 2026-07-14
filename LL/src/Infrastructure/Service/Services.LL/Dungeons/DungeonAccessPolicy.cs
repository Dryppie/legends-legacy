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
        int currentCombatRating,
        CancellationToken cancellationToken)
        => await EvaluateAsync(characterId, dungeon, currentCombatRating, ignoredEntryCostItemId: null, cancellationToken);

    public async Task<DungeonAccessResult> EvaluateForSigilForgeAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        int currentCombatRating,
        CancellationToken cancellationToken) =>
        await EvaluateAsync(characterId, dungeon, currentCombatRating, dungeon.SigilItemId, cancellationToken);

    private async Task<DungeonAccessResult> EvaluateAsync(
        Guid characterId,
        DungeonDefinition dungeon,
        int currentCombatRating,
        string? ignoredEntryCostItemId,
        CancellationToken cancellationToken)
    {
        var missingRequirements = new List<string>();
        var entryRequirements = await GetEntryRequirementsAsync(
            characterId,
            dungeon,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
            && !await _dungeonRuns.HasCompletedDungeonAsync(
                characterId,
                dungeon.RequiredPreviousDungeonId,
                cancellationToken))
        {
            missingRequirements.Add("Complete the previous difficulty first.");
        }

        AddMissingEntryCosts(entryRequirements, missingRequirements, ignoredEntryCostItemId);

        return new DungeonAccessResult(
            missingRequirements.Count == 0,
            missingRequirements,
            entryRequirements,
            currentCombatRating,
            dungeon.RecommendedCombatRating);
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

            var itemName = itemBases.TryGetValue(cost.ItemId, out var itemBase)
                ? itemBase.Name
                : cost.ItemId;

            requirements.Add(new DungeonEntryRequirementResult(
                cost.ItemId,
                itemName,
                cost.Amount,
                owned));
        }

        return requirements;
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
