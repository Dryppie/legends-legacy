using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonRunRewardClaimer : IDungeonRunRewardClaimer
{
    private readonly IExperienceRewardWriter _experienceWriter;
    private readonly ILootRewardWriter _lootWriter;
    private readonly ICurrencyRewardWriter _currencyWriter;
    private readonly IItemBaseRepository _itemBases;

    public DungeonRunRewardClaimer(
        IExperienceRewardWriter experienceWriter,
        ILootRewardWriter lootWriter,
        ICurrencyRewardWriter currencyWriter,
        IItemBaseRepository itemBases)
    {
        _experienceWriter = experienceWriter;
        _lootWriter = lootWriter;
        _currencyWriter = currencyWriter;
        _itemBases = itemBases;
    }

    public async Task ClaimAsync(DungeonRun run, CancellationToken cancellationToken)
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
            return;
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

            inventoryItems.AddRange(CreateInventoryItems(run.CharacterId, itemBase, reward.Quantity));
        }

        if (inventoryItems.Count > 0)
        {
            await _lootWriter.AddLootAsync(
                run.CharacterId,
                inventoryItems,
                cancellationToken);
        }
    }

    private static IEnumerable<InventoryItem> CreateInventoryItems(Guid characterId, ItemBase itemBase, int quantity)
    {
        if (itemBase.Stackable)
        {
            yield return CreateInventoryItem(characterId, itemBase, quantity);
            yield break;
        }

        for (var i = 0; i < quantity; i++)
        {
            yield return CreateInventoryItem(characterId, itemBase, 1);
        }
    }

    private static InventoryItem CreateInventoryItem(Guid characterId, ItemBase itemBase, int quantity)
    {
        var itemInstance = CreateItemInstance(itemBase);

        return new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            Quantity = quantity,
            ItemInstance = itemInstance
        };
    }

    private static ItemInstance CreateItemInstance(ItemBase itemBase)
    {
        return itemBase.ItemType switch
        {
            ItemType.Equipment => new EquipmentInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            ItemType.Essence => new EssenceItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            },
            _ => new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            }
        };
    }
}
