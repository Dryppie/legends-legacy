using Application.Interfaces.Services.LL;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonRunRewardClaimer : IDungeonRunRewardClaimer
{
    private readonly IExperienceRewardWriter _experienceWriter;
    private readonly ICurrencyRewardWriter _currencyWriter;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IInventoryService _inventoryService;

    public DungeonRunRewardClaimer(
        IExperienceRewardWriter experienceWriter,
        ICurrencyRewardWriter currencyWriter,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory,
        IInventoryService inventoryService)
    {
        _experienceWriter = experienceWriter;
        _currencyWriter = currencyWriter;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
        _inventoryService = inventoryService;
    }

    public async Task<IReadOnlyList<InventoryItem>> ClaimAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        var rewardState = GetClaimableRewards(run);

        if (rewardState.Experience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                [run.CharacterId],
                rewardState.Experience,
                cancellationToken);
        }

        if (rewardState.Cinders > 0 || rewardState.Soulstones > 0)
        {
            await _currencyWriter.AddAsync(
                run.CharacterId,
                rewardState.Cinders,
                rewardState.Soulstones,
                cancellationToken);
        }

        if (rewardState.Items.Count <= 0)
        {
            return [];
        }

        var itemIds = rewardState.Items
            .Where(x => x.Value > 0)
            .Select(x => x.Key)
            .Distinct()
            .ToArray();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            itemIds,
            cancellationToken);

        var inventoryItems = new List<InventoryItem>();
        foreach (var reward in rewardState.Items)
        {
            if (reward.Value <= 0 || !itemBases.TryGetValue(reward.Key, out var itemBase))
            {
                continue;
            }

            inventoryItems.AddRange(_inventoryItemFactory.CreateForQuantity(itemBase, reward.Value, run.CharacterId));
        }

        if (inventoryItems.Count > 0)
        {
            await _inventoryService.AddItemsToInventory(
                run.CharacterId,
                inventoryItems.ToList(),
                ItemAcquisitionSources.DungeonReward,
                cancellationToken);
        }

        return inventoryItems;
    }

    private static DungeonLootBag GetClaimableRewards(DungeonRun run)
    {
        if (run.Status == DungeonRunStatus.Retreated && HasLoot(run.State?.SecuredLoot))
        {
            return run.State!.SecuredLoot;
        }

        return new DungeonLootBag
        {
            Experience = run.PendingExperience,
            Cinders = run.PendingCinders,
            Soulstones = run.PendingSoulstones,
            Items = run.PendingRewards
                .Where(x => !string.IsNullOrWhiteSpace(x.ItemId) && x.Quantity > 0)
                .GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Sum(reward => reward.Quantity), StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool HasLoot(DungeonLootBag? bag) =>
        bag is not null &&
        (bag.Experience > 0 ||
            bag.Cinders > 0 ||
            bag.Soulstones > 0 ||
            bag.Items.Any(x => x.Value > 0));
}
