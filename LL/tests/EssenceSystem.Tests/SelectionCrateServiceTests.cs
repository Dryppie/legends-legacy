using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Commands.OpenCatalystSelectionCrate;
using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.LootHistory.Dtos;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Services.LL.Inventories;
using Microsoft.Extensions.Logging.Abstractions;

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
        Assert.Equal("Catalyst Selection Cache", result.ContainerName);
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
        Assert.Equal("Blueprint Selection Box", result.ContainerName);
        Assert.Equal(0, box.Quantity);
        var reward = Assert.Single(result.Rewards);
        Assert.Equal("blueprint_primal", reward.ItemInstance.ItemBaseId);
        Assert.Equal(1, reward.Quantity);
    }

    [Fact]
    public async Task OpeningSelectionContainerRecordsAndPublishesLootWithContainerOrigin()
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
        var lootHistory = new RecordingLootHistoryService();
        var legacy = new RecordingGameEventPublisher();
        var realtime = new RecordingRealtimeBroadcaster();
        var handler = new OpenCatalystSelectionCrateCommandHandler(
            new SelectionCrateService(inventory, itemBases, new InventoryItemFactory()),
            lootHistory,
            legacy,
            realtime,
            CreateMapper());

        var response = await handler.Handle(
            new OpenCatalystSelectionCrateCommand(characterId, crate.ItemInstanceId, "fury"),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("container-reward", lootHistory.Source);
        Assert.Equal("Catalyst Selection Cache", lootHistory.Location);
        Assert.Equal(6, Assert.Single(lootHistory.Items!).Quantity);

        var legacyMessage = Assert.IsType<LootReceivedMsg>(legacy.Message);
        var realtimeMessage = Assert.IsType<LootReceived>(realtime.Message);
        Assert.Equal(response.Data.GrantId, legacyMessage.GrantId);
        Assert.Equal(response.Data.GrantId, realtimeMessage.GrantId);
        Assert.Equal("Catalyst Selection Cache", realtimeMessage.Location);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            options => options.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
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

        public Task AddItemsToInventory(
            Guid characterId,
            List<InventoryItem> loot,
            string acquisitionSource,
            CancellationToken cancellationToken)
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
        public Task<InventoryTransferResult> TransferItemAsync(Guid senderCharacterId, Guid recipientCharacterId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken) => throw new NotSupportedException();
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

    private sealed class RecordingLootHistoryService : ILootHistoryService
    {
        public IReadOnlyCollection<Application.UseCases.Inventories.Dtos.InventoryItemDto>? Items { get; private set; }
        public string? Source { get; private set; }
        public string? Location { get; private set; }

        public Task RecordAsync(
            Guid characterId,
            IReadOnlyCollection<Application.UseCases.Inventories.Dtos.InventoryItemDto> items,
            string source,
            string? location,
            CancellationToken cancellationToken)
        {
            Items = items;
            Source = source;
            Location = location;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LootHistoryEntryDto>> GetRecentAsync(
            Guid characterId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> ClearAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingGameEventPublisher : IGameEventPublisher
    {
        public GameEventMsg? Message { get; private set; }

        public Task PublishAsync(Audience audience, GameEventMsg message)
        {
            Message = message;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public GameRealtimeEvent? Message { get; private set; }

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.CompletedTask;
        }
    }
}
