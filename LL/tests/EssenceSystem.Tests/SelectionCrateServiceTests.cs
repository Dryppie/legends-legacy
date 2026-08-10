using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed class SelectionCrateServiceTests
{
    [Fact]
    public async Task OpeningCatalystCrateConsumesOneCrateAndGrantsSelectedBundle()
    {
        var characterId = Guid.NewGuid();
        var crate = CreateInventoryItem(
            characterId,
            CatalystSelectionCrateCatalog.ItemBaseId,
            ItemType.Resource,
            quantity: 1);
        var inventory = new FakeInventoryService(crate);
        var itemBases = new FakeItemBaseRepository(CatalystSelectionCrateCatalog.Options.Select(option =>
            new ItemBase
            {
                Id = option.ItemId,
                Name = option.Name,
                ItemType = ItemType.Resource,
                Stackable = true
            }));
        var service = new SelectionCrateService(
            inventory,
            itemBases,
            new InventoryItemFactory());

        var result = await service.OpenSelectionContainerAsync(
            characterId,
            crate.ItemInstanceId,
            "fury",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, crate.Quantity);
        var reward = Assert.Single(result.Rewards);
        Assert.Equal("fury_heart", reward.ItemInstance.ItemBaseId);
        Assert.Equal(6, reward.Quantity);
        Assert.Single(inventory.AddedRewards);
    }

    [Fact]
    public async Task InvalidCatalystChoiceDoesNotConsumeCrate()
    {
        var characterId = Guid.NewGuid();
        var crate = CreateInventoryItem(
            characterId,
            CatalystSelectionCrateCatalog.ItemBaseId,
            ItemType.Resource,
            quantity: 1);
        var inventory = new FakeInventoryService(crate);
        var service = new SelectionCrateService(
            inventory,
            new FakeItemBaseRepository([]),
            new InventoryItemFactory());

        var result = await service.OpenSelectionContainerAsync(
            characterId,
            crate.ItemInstanceId,
            "unknown",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, crate.Quantity);
        Assert.Empty(inventory.AddedRewards);
    }

    [Fact]
    public async Task OpeningBlueprintBoxConsumesOneBoxAndGrantsSelectedBlueprint()
    {
        var characterId = Guid.NewGuid();
        var box = CreateInventoryItem(
            characterId,
            BlueprintSelectionBoxCatalog.ItemBaseId,
            ItemType.Resource,
            quantity: 1);
        var inventory = new FakeInventoryService(box);
        var itemBases = new FakeItemBaseRepository(BlueprintSelectionBoxCatalog.Options.Select(option =>
            new ItemBase
            {
                Id = option.ItemId,
                Name = option.Name,
                ItemType = ItemType.Resource,
                Stackable = true
            }));
        var service = new SelectionCrateService(
            inventory,
            itemBases,
            new InventoryItemFactory());

        var result = await service.OpenSelectionContainerAsync(
            characterId,
            box.ItemInstanceId,
            "primal",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, box.Quantity);
        var reward = Assert.Single(result.Rewards);
        Assert.Equal("blueprint_primal", reward.ItemInstance.ItemBaseId);
        Assert.Equal(1, reward.Quantity);
    }

    private static InventoryItem CreateInventoryItem(
        Guid characterId,
        string itemBaseId,
        ItemType itemType,
        int quantity)
    {
        var itemBase = new ItemBase
        {
            Id = itemBaseId,
            Name = itemBaseId,
            ItemType = itemType,
            Stackable = true
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBaseId,
            ItemBase = itemBase
        };
        return new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = quantity
        };
    }

    private sealed class FakeInventoryService(InventoryItem crate) : IInventoryService
    {
        public List<InventoryItem> AddedRewards { get; } = [];

        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult<InventoryItem?>(
                crate.InventoryId == characterId && crate.ItemInstanceId == itemInstanceId && crate.Quantity > 0
                    ? crate
                    : null);

        public Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken)
        {
            if (crate.InventoryId != characterId || crate.ItemInstanceId != itemInstanceId || crate.Quantity <= 0)
            {
                return Task.FromResult(false);
            }

            crate.Quantity--;
            return Task.FromResult(true);
        }

        public Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
        {
            AddedRewards.AddRange(loot);
            return Task.CompletedTask;
        }

        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeItemBaseRepository(IEnumerable<ItemBase> itemBases) : IItemBaseRepository
    {
        private readonly IReadOnlyDictionary<string, ItemBase> _itemBases = itemBases.ToDictionary(item => item.Id);

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                _itemBases
                    .Where(pair => itemIds.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value));

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
