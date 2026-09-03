using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using AutoMapper;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.MarketPlaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.LL.Items;
using Services.LL.MarketPlaces;

namespace EssenceSystem.Tests;

public sealed partial class MarketPlaceServiceTests
{
    [Theory]
    [InlineData(true, false)]
    public async Task Exact_legacy_equipment_purchase_respects_buyer_cohort(bool modern, bool allowed)
    {
        var seller = new Character { Id = Guid.NewGuid(), Cinders = 10 };
        var buyer = new Character { Id = Guid.NewGuid(), Cinders = 500 };
        var itemBase = new EquipmentBase { Id = "old_sword", Name = "Old Sword" };
        var item = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = itemBase.Id, ItemBase = itemBase };
        var listing = MarketListing(seller.Id, item);
        var repository = new FakeMarketRepository();
        repository.Listings.Add(listing);
        var inventory = new FakeInventoryService(null);
        var service = EquipmentProgressionMarketService(repository, inventory, modern, buyer, seller);
        var result = await service.BuyoutMarketPlaceListingAsync(buyer.Id, listing.Id, 1, default);
        Assert.Equal(allowed, result is not null);
        Assert.Equal(allowed ? 400 : 500, buyer.Cinders);
        if (!allowed)
        {
            Assert.Equal(10, seller.Cinders);
            Assert.Equal(1, listing.Quantity);
            Assert.Single(repository.Listings);
            Assert.Empty(repository.Orders);
            Assert.Empty(inventory.MarketPurchases);
            Assert.Single(await service.GetMarketPlaceListingsAsync(default)); // Shared snapshot still supports owner cancellation.
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Canonical_listing_metadata_and_purchase_keep_the_exact_instance(bool modern)
    {
        var seller = new Character { Id = Guid.NewGuid(), Cinders = 0 };
        var buyer = new Character { Id = Guid.NewGuid(), Cinders = 500 };
        var item = MarketEquipment(seller.Id, EquipmentOwnershipKind.UnboundPersonal);
        var before = item.ProgressionData!;
        var listing = MarketListing(seller.Id, item);
        var repository = new FakeMarketRepository();
        repository.Listings.Add(listing);
        var inventory = new FakeInventoryService(null);
        var service = EquipmentProgressionMarketService(repository, inventory, modern, buyer, seller);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<MarketPlaceListingDto>(Assert.Single(await service.GetMarketPlaceListingsAsync(default)));
        var equipment = Assert.IsType<EquipmentInstanceDto>(dto.ItemInstance);
        Assert.Equal(before.State.DefinitionId, equipment.Progression!.DefinitionId);
        Assert.Equal(before.State.ArchetypeId, equipment.Progression.ArchetypeId);
        Assert.Equal(before.State.Rank, equipment.Progression.Rank);
        Assert.Equal(before.State.NativeStyleId, equipment.Progression.NativeStyleId);
        Assert.Equal(before.State.ActiveStyleId, equipment.Progression.ActiveStyleId);
        Assert.Equal(before.State.Ownership.Kind, equipment.Progression.Ownership);
        Assert.False(equipment.IsBound);
        Assert.NotNull(await service.BuyoutMarketPlaceListingAsync(buyer.Id, listing.Id, 1, default));
        var purchased = Assert.Single(inventory.MarketPurchases);
        Assert.Same(item, purchased.ItemInstance);
        Assert.Equal(before, item.ProgressionData);
        Assert.Equal(400, buyer.Cinders);
        Assert.Single(repository.Orders);
        Assert.Empty(repository.Listings);
        Assert.Null(await service.BuyoutMarketPlaceListingAsync(buyer.Id, listing.Id, 1, default));
        Assert.Single(inventory.MarketPurchases);
    }

    [Theory]
    [InlineData(EquipmentOwnershipKind.BoundPersonal)]
    [InlineData(EquipmentOwnershipKind.GuildOwned)]
    public async Task Canonical_bound_or_guild_equipment_cannot_be_bought_or_escrowed(EquipmentOwnershipKind ownership)
    {
        var seller = new Character { Id = Guid.NewGuid(), Cinders = 0 };
        var buyer = new Character { Id = Guid.NewGuid(), Cinders = 500 };
        var item = MarketEquipment(seller.Id, ownership);
        var listing = MarketListing(seller.Id, item);
        var repository = new FakeMarketRepository();
        repository.Listings.Add(listing);
        var inventory = new FakeInventoryService(new() { InventoryId = seller.Id, ItemInstanceId = item.Id, ItemInstance = item, Quantity = 1 });
        var service = EquipmentProgressionMarketService(repository, inventory, true, buyer, seller);
        Assert.Null(await service.BuyoutMarketPlaceListingAsync(buyer.Id, listing.Id, 1, default));
        Assert.Null(await service.CreateMarketPlaceListingAsync(seller.Id, listing, default));
        Assert.Equal(500, buyer.Cinders);
        Assert.Equal(0, seller.Cinders);
        Assert.Empty(inventory.MarketPurchases);
        Assert.Equal(0, inventory.RemoveForListingCalls);
        Assert.Empty(repository.Orders);
    }

    private static MarketPlaceService EquipmentProgressionMarketService(FakeMarketRepository repository, FakeInventoryService inventory,
        bool modern, params Character[] characters) => new(repository, new FakeItemBaseRepository([]), inventory,
        new FakeCharacterService(characters), Options.Create(new MarketPlaceOptions()), TimeProvider.System);

    private static MarketPlaceListing MarketListing(Guid sellerId, EquipmentInstance item) => new()
    {
        Id = Guid.NewGuid(), SellerId = sellerId, ItemInstanceId = item.Id, ItemInstance = item, Quantity = 1,
        UnitPrice = 100, CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
    };

    private static EquipmentInstance MarketEquipment(Guid ownerId, EquipmentOwnershipKind ownership)
    {
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(EquipmentProgressionSharedContentTests.ApiRoot(), "Data/equipment/equipment-starters.v1.json"));
        var definition = catalog.Evaluator.Definitions.First(x => x.NativeStyleId == "blueprint_fury");
        var state = EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, definition.Id, 1, 3,
            new(EquipmentAwardKind.RandomDiscovery, "market-test", Guid.NewGuid().ToString("N")), new(ownership, ownerId));
        var data = EquipmentData.Create(state, catalog.Evaluator);
        var itemBase = new EquipmentBase { Id = data.ItemBaseId, Name = data.DisplayName, EquipmentType = data.EquipmentType };
        var item = new EquipmentInstance { Id = data.State.Id, ItemBaseId = itemBase.Id, ItemBase = itemBase };
        item.ApplyProgressionData(data);
        return item;
    }
}
