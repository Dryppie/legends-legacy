using Domain.Models.Items.Equipments.Progression;
using Application.Interfaces.Services.LL;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
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
    private readonly Application.Interfaces.Services.LL.Items.IEquipmentAcquisitionService? _progression;

    public DungeonRunRewardClaimer(
        IExperienceRewardWriter experienceWriter,
        ICurrencyRewardWriter currencyWriter,
        IItemBaseRepository itemBases,
        IInventoryItemFactory inventoryItemFactory,
        IInventoryService inventoryService,
        Application.Interfaces.Services.LL.Items.IEquipmentAcquisitionService? progression = null)
    {
        _experienceWriter = experienceWriter;
        _currencyWriter = currencyWriter;
        _itemBases = itemBases;
        _inventoryItemFactory = inventoryItemFactory;
        _inventoryService = inventoryService;
        _progression = progression;
    }

    public async Task<IReadOnlyList<InventoryItem>> ClaimAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        var rewardState = GetClaimableRewards(run);
        var equipmentProgressionRewards = run.Status == DungeonRunStatus.Completed
            ? run.PendingRewards.Where(x => x.ProgressionData != null).ToArray() : [];
        var frozenBases = await _itemBases.GetItemBasesByIdsAsync(equipmentProgressionRewards.Select(x => x.ItemId).Distinct().ToArray(), cancellationToken);
        if (equipmentProgressionRewards.Any(x => x.Quantity != 1 || x.ProgressionData!.ItemBaseId != x.ItemId
            || x.ProgressionData.State.Ownership.OwnerId != run.CharacterId || !frozenBases.TryGetValue(x.ItemId, out var itemBase)
            || itemBase is not EquipmentBase equipmentBase || equipmentBase.Stackable || equipmentBase.EquipmentType != x.ProgressionData.EquipmentType))
            throw new InvalidOperationException("The frozen dungeon equipment reward is unavailable or invalid.");

        if (rewardState.Experience > 0)
        {
            await _experienceWriter.AddSplitExperienceAsync(
                [run.CharacterId],
                rewardState.Experience,
                EssenceCombatActivity.Dungeon,
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

        if (rewardState.Items.Count <= 0 && equipmentProgressionRewards.Length == 0)
        {
            if (_progression != null) await _progression.MarkClaimedAsync(run, cancellationToken);
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
        foreach (var reward in equipmentProgressionRewards)
        {
            var data = reward.ProgressionData!;
            var instance = new EquipmentInstance { Id = data.State.Id, ItemBaseId = data.ItemBaseId, ItemBase = frozenBases[data.ItemBaseId],
                AcquisitionSource = ItemAcquisitionSources.DungeonReward, AcquiredAtUtc = run.CompletedAt ?? run.CreatedAt };
            instance.ApplyProgressionData(data);
            inventoryItems.Add(new InventoryItem { InventoryId = run.CharacterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 1 });
        }
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

        if (_progression != null) await _progression.MarkClaimedAsync(run, cancellationToken);
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
                .Where(x => x.ProgressionData == null && !string.IsNullOrWhiteSpace(x.ItemId) && x.Quantity > 0)
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
