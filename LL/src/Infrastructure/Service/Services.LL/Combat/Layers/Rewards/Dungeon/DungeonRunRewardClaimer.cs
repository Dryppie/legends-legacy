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
    private readonly ILootRewardWriter _lootWriter;
    private readonly ICurrencyRewardWriter _currencyWriter;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryItemFactory _inventoryItemFactory;

    public DungeonRunRewardClaimer(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
    }

    public async Task<IReadOnlyList<InventoryItem>> ClaimAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        if (run.PendingExperience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                [run.CharacterId],
                run.PendingExperience,
                cancellationToken);
        }

        if (run.PendingCinders > 0 || run.PendingSoulstones > 0)
        {
            await _currencyWriter.AddAsync(
                run.CharacterId,
                run.PendingCinders,
                run.PendingSoulstones,
                cancellationToken);
        }

        if (run.PendingRewards.Count <= 0)
        {
            return [];
        }

        var itemIds = run.PendingRewards
            .Select(x => x.ItemId)
            .Distinct()
            .ToArray();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(
            itemIds,
            cancellationToken);

        var inventoryItems = new List<InventoryItem>();
        foreach (var reward in run.PendingRewards)
        {
            if (!itemBases.TryGetValue(reward.ItemId, out var itemBase))
            {
                continue;
            }

            inventoryItems.AddRange(_inventoryItemFactory.CreateForQuantity(itemBase, reward.Quantity, run.CharacterId));
        }

        if (inventoryItems.Count > 0)
        {
            await _lootWriter.AddLootAsync(
                run.CharacterId,
                inventoryItems,
                cancellationToken);
        }

        return inventoryItems;
    }

}
