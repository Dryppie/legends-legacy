using Application.Common.Mappings;
using System.Text.Json;
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
using Domain.Models.Items.EssenceItems;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Services.LL.Inventories;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed partial class SelectionCrateServiceTests
{
    [Fact]
    public void ShenicEssenceTokensOfferFiveAreaEssencesEach()
    {
        Assert.Equal(10, ShenicEssenceTokenCatalog.Definitions.Count);
        Assert.Equal(
            10,
            ShenicEssenceTokenCatalog.Definitions
                .Select(definition => definition.ItemBaseId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.All(ShenicEssenceTokenCatalog.Definitions, definition =>
        {
            Assert.Equal("Essence", definition.SelectionLabel);
            Assert.Equal(5, definition.Options.Count);
            Assert.All(definition.Options, option =>
            {
                Assert.StartsWith("item.essence.", option.ItemId);
                Assert.EndsWith(" Essence", option.Name);
                Assert.Equal(1, option.Quantity);
            });
        });
    }

    [Fact]
    public async Task ShenicEssenceTokenCatalogMatchesAreaCreatureAndItemCatalogs()
    {
        var dataRoot = Path.Combine(TestContentPaths.FindApiRoot(), "Data");
        using var regionDocument = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(dataRoot, "world", "regions.json")));
        using var creatureDocument = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(dataRoot, "world", "creatures.json")));
        using var itemDocument = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(dataRoot, "items", "items.json")));

        var creaturesById = creatureDocument.RootElement
            .GetProperty("creatures")
            .EnumerateArray()
            .ToDictionary(creature => creature.GetProperty("id").GetGuid());
        var itemIds = itemDocument.RootElement
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shenicAreas = regionDocument.RootElement
            .GetProperty("regions")
            .EnumerateArray()
            .Single(region => region.GetProperty("name").GetString() == "Shenic")
            .GetProperty("areas")
            .EnumerateArray()
            .Where(area => area.GetProperty("id").GetString() != "tutorial_area_training_grounds")
            .ToList();

        Assert.Equal(ShenicEssenceTokenCatalog.Definitions.Count, shenicAreas.Count);
        foreach (var area in shenicAreas)
        {
            var areaName = area.GetProperty("name").GetString()!;
            var areaKey = areaName.ToLowerInvariant().Replace(' ', '_');
            var definition = Assert.Single(ShenicEssenceTokenCatalog.Definitions, candidate =>
                candidate.ItemBaseId == ShenicEssenceTokenCatalog.ItemBaseId(areaKey));
            var expectedEssenceItemIds = area
                .GetProperty("creatures")
                .EnumerateArray()
                .Select(spawn => creaturesById[spawn.GetProperty("creatureId").GetGuid()])
                .Select(creature => $"item.essence.{creature.GetProperty("imagePath").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Equal($"{areaName} - Essence Token", definition.DisplayName);
            Assert.Equal(
                expectedEssenceItemIds.Order(StringComparer.OrdinalIgnoreCase),
                definition.Options.Select(option => option.ItemId).Order(StringComparer.OrdinalIgnoreCase));
            Assert.Contains(definition.ItemBaseId, itemIds);
            Assert.All(definition.Options, option => Assert.Contains(option.ItemId, itemIds));
        }
    }

    [Fact]
    public async Task OpeningShenicEssenceTokenConsumesTokenAndGrantsSelectedUnboundEssence()
    {
        var characterId = Guid.NewGuid();
        var tokenDefinition = ShenicEssenceTokenCatalog.Definitions.Single(definition =>
            definition.ItemBaseId == "item.essence_token.blood_grove");
        var token = CreateInventoryItem(
            characterId,
            tokenDefinition.ItemBaseId,
            ItemType.Resource,
            quantity: 1);
        var inventory = new FakeInventoryService(token);
        var itemBases = new FakeItemBaseRepository(tokenDefinition.Options.Select(option =>
            new EssenceItemBase
            {
                Id = option.ItemId,
                Name = option.Name,
                ItemType = ItemType.Essence,
                Stackable = false
            }));
        var service = new SelectionCrateService(
            inventory,
            itemBases,
            new InventoryItemFactory());

        var result = await service.OpenSelectionContainerAsync(
            characterId,
            token.ItemInstanceId,
            "nightshade_blossom",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Blood Grove - Essence Token", result.ContainerName);
        Assert.Equal(0, token.Quantity);
        var reward = Assert.Single(result.Rewards);
        Assert.Equal("item.essence.nightshade_blossom", reward.ItemInstance.ItemBaseId);
        Assert.IsType<EssenceItemInstance>(reward.ItemInstance);
        Assert.Equal(1, reward.Quantity);
    }


    [Fact]
    public async Task OpeningSelectionContainerRecordsAndPublishesLootWithContainerOrigin()
    {
        var characterId = Guid.NewGuid();
        var crate = CreateInventoryItem(
            characterId,
            ShenicEssenceTokenCatalog.ItemBaseId("lumo_ruins"),
            ItemType.Resource,
            quantity: 1);
        var inventory = new FakeInventoryService(crate);
        var itemBases = new FakeItemBaseRepository(ShenicEssenceTokenCatalog.Definitions[0].Options.Select(option =>
            new ItemBase
            {
                Id = option.ItemId,
                Name = option.Name,
                ItemType = ItemType.Resource,
                Stackable = true
            }));
        var lootHistory = new RecordingLootHistoryService();
        var realtime = new RecordingRealtimeBroadcaster();
        var handler = new OpenCatalystSelectionCrateCommandHandler(
            new SelectionCrateService(inventory, itemBases, new InventoryItemFactory()),
            inventory,
            lootHistory,
            realtime,
            CreateMapper());

        var response = await handler.Handle(
            new OpenCatalystSelectionCrateCommand(characterId, crate.ItemInstanceId, "goblin"),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("container-reward", lootHistory.Source);
        Assert.Equal("Lumo Ruins - Essence Token", lootHistory.Location);
        Assert.Equal(1, Assert.Single(lootHistory.Items!).Quantity);

        var realtimeMessage = Assert.IsType<LootReceived>(realtime.Message);
        Assert.Equal(response.Data.GrantId, realtimeMessage.GrantId);
        Assert.Equal("Lumo Ruins - Essence Token", realtimeMessage.Location);
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

        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
        {
            if (crate.InventoryId != characterId)
            {
                return Task.FromResult<Inventory?>(null);
            }

            var items = AddedRewards.ToList();
            if (crate.Quantity > 0)
            {
                items.Add(crate);
            }

            return Task.FromResult<Inventory?>(new Inventory
            {
                CharacterId = characterId,
                InventoryItems = items
            });
        }
        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetItemFavoriteAsync(Guid characterId, Guid itemInstanceId, bool isFavorite, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<InventoryItem?> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) => throw new NotSupportedException();
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
