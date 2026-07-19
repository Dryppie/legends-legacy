using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Microsoft.Extensions.Options;
using Services.LL.MarketPlaces;

namespace EssenceSystem.Tests;

public sealed class MarketPlaceServiceTests
{
    [Fact]
    public async Task CreateListing_RejectsMultipleCopiesOfUniqueItemBeforeEscrow()
    {
        var equipmentBase = new EquipmentBase { Id = "sword", Name = "Sword" };
        var inventory = new FakeInventoryService(CreateInventoryItem(equipmentBase, 1));
        var service = CreateService(new FakeMarketRepository(), inventory, [equipmentBase]);

        var result = await service.CreateMarketPlaceListingAsync(
            Guid.NewGuid(),
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 2,
                UnitPrice = 100
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, inventory.RemoveForListingCalls);
    }

    [Fact]
    public async Task CreateListing_RejectsFullOrderBookBeforeEscrow()
    {
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 20));
        var market = new FakeMarketRepository { ListingCount = 10 };
        var service = CreateService(market, inventory, [resource]);

        var result = await service.CreateMarketPlaceListingAsync(
            Guid.NewGuid(),
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 5,
                UnitPrice = 10
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, inventory.RemoveForListingCalls);
    }

    [Fact]
    public async Task CreateListing_RejectsActiveBuyOrderForTheSameItemBeforeEscrow()
    {
        var characterId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 20));
        var market = new FakeMarketRepository();
        market.BuyOrders.Add(new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = characterId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 5,
            UnitPrice = 10,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var service = CreateService(market, inventory, [resource]);

        var result = await service.CreateMarketPlaceListingAsync(
            characterId,
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 5,
                UnitPrice = 10
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, inventory.RemoveForListingCalls);
        Assert.Empty(market.Listings);
    }

    [Fact]
    public async Task CreateListing_RejectsSecondActiveListingForTheSameStackableItem()
    {
        var characterId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var listedItem = CreateInventoryItem(resource, 5);
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 20));
        var market = new FakeMarketRepository();
        market.Listings.Add(new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = characterId,
            ItemInstanceId = listedItem.ItemInstanceId,
            ItemInstance = listedItem.ItemInstance,
            Quantity = 5,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var service = CreateService(
            market,
            inventory,
            [resource],
            new Character { Id = characterId });

        var result = await service.CreateMarketPlaceListingAsync(
            characterId,
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 5,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, inventory.RemoveForListingCalls);
        Assert.Single(market.Listings);
    }

    [Fact]
    public async Task CreateListing_AllowsActiveBuyOrderForADifferentItem()
    {
        var characterId = Guid.NewGuid();
        var ore = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var wood = new ItemBase { Id = "wood", Name = "Wood", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(ore, 20));
        var market = new FakeMarketRepository();
        market.BuyOrders.Add(new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = characterId,
            ItemBaseId = wood.Id,
            ItemBase = wood,
            Quantity = 5,
            UnitPrice = 10,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var service = CreateService(
            market,
            inventory,
            [ore, wood],
            new Character { Id = characterId });

        var result = await service.CreateMarketPlaceListingAsync(
            characterId,
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 5,
                UnitPrice = 10
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, inventory.RemoveForListingCalls);
        Assert.Single(market.Listings);
    }

    [Fact]
    public async Task CreateListing_FillsHighestBuyOrdersAndListsOnlyTheRemainder()
    {
        var sellerId = Guid.NewGuid();
        var firstBuyerId = Guid.NewGuid();
        var secondBuyerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 6));
        var now = DateTimeOffset.UtcNow;
        var highestOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = firstBuyerId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 2,
            UnitPrice = 12,
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        };
        var nextOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = secondBuyerId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 1,
            UnitPrice = 11,
            CreatedAt = now.AddMinutes(1),
            ExpiresAt = now.AddDays(1)
        };
        var belowLimitOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 5,
            UnitPrice = 9,
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        };
        var market = new FakeMarketRepository();
        market.BuyOrders.AddRange([nextOrder, belowLimitOrder, highestOrder]);
        var seller = new Character { Id = sellerId, Cinders = 0 };
        var service = CreateService(market, inventory, [resource], seller);

        var result = await service.CreateMarketPlaceListingAsync(
            sellerId,
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 6,
                UnitPrice = 10
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.FilledQuantity);
        Assert.Equal(35, result.FilledTotalPrice);
        Assert.Equal(2, result.SellerFees);
        Assert.Equal(33, result.SellerCinders);
        Assert.Equal(33, seller.Cinders);
        Assert.Equal(3, result.Listing?.Quantity);
        Assert.Equal(10, result.Listing?.UnitPrice);
        Assert.Single(market.Listings);
        Assert.Same(belowLimitOrder, Assert.Single(market.BuyOrders));
        Assert.Equal(3, inventory.MarketPurchases.Sum(x => x.Quantity));
        Assert.Collection(
            market.Orders,
            trade =>
            {
                Assert.Equal(2, trade.Quantity);
                Assert.Equal(12, trade.UnitPrice);
                Assert.Equal(24, trade.TotalPrice);
                Assert.Equal(1, trade.SellerFee);
            },
            trade =>
            {
                Assert.Equal(1, trade.Quantity);
                Assert.Equal(11, trade.UnitPrice);
                Assert.Equal(11, trade.TotalPrice);
                Assert.Equal(1, trade.SellerFee);
            });
    }

    [Fact]
    public async Task CreateListing_WhenFullyCrossed_DoesNotConsumeAListingSlot()
    {
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 2));
        var now = DateTimeOffset.UtcNow;
        var market = new FakeMarketRepository { ListingCount = 10 };
        market.BuyOrders.Add(new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 2,
            UnitPrice = 12,
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        });
        var seller = new Character { Id = sellerId, Cinders = 0 };
        var service = CreateService(market, inventory, [resource], seller);

        var result = await service.CreateMarketPlaceListingAsync(
            sellerId,
            new MarketPlaceListing
            {
                ItemInstanceId = inventory.Item!.ItemInstanceId,
                Quantity = 2,
                UnitPrice = 10
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Listing);
        Assert.Equal(2, result.FilledQuantity);
        Assert.Equal(24, result.FilledTotalPrice);
        Assert.Empty(market.Listings);
        Assert.Empty(market.BuyOrders);
        Assert.Single(market.Orders);
    }

    [Fact]
    public async Task BuyoutListing_DeductsConfiguredFeeAndRecordsTrade()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var instance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = resource.Id,
            ItemBase = resource
        };
        var listing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            ItemInstanceId = instance.Id,
            ItemInstance = instance,
            Quantity = 3,
            UnitPrice = 100,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var buyer = new Character { Id = buyerId, Cinders = 1_000 };
        var seller = new Character { Id = sellerId, Cinders = 0 };
        var market = new FakeMarketRepository();
        market.Listings.Add(listing);
        var inventory = new FakeInventoryService(null);
        var service = CreateService(market, inventory, [resource], buyer, seller);

        var result = await service.BuyoutMarketPlaceListingAsync(
            buyerId,
            listing.Id,
            2,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(800, buyer.Cinders);
        Assert.Equal(194, seller.Cinders);
        var trade = Assert.Single(market.Orders);
        Assert.Equal(200, trade.TotalPrice);
        Assert.Equal(6, trade.SellerFee);
        Assert.Equal(2, trade.Quantity);
        Assert.Equal(1, listing.Quantity);
    }

    [Fact]
    public async Task BuyCommodity_MixedOwnershipAtOnePrice_SkipsOwnQuantity()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var ownListedItem = CreateInventoryItem(resource, 5);
        var otherListedItem = CreateInventoryItem(resource, 3);
        var now = DateTimeOffset.UtcNow;
        var ownListing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = buyerId,
            ItemInstanceId = ownListedItem.ItemInstanceId,
            ItemInstance = ownListedItem.ItemInstance,
            Quantity = 5,
            UnitPrice = 10,
            CreatedAt = now,
            ExpiresAt = now.AddDays(1)
        };
        var otherListing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            ItemInstanceId = otherListedItem.ItemInstanceId,
            ItemInstance = otherListedItem.ItemInstance,
            Quantity = 3,
            UnitPrice = 10,
            CreatedAt = now.AddSeconds(1),
            ExpiresAt = now.AddDays(1)
        };
        var market = new FakeMarketRepository();
        market.Listings.AddRange([ownListing, otherListing]);
        var inventory = new FakeInventoryService(null);
        var buyer = new Character { Id = buyerId, Cinders = 100 };
        var seller = new Character { Id = sellerId, Cinders = 0 };
        var service = CreateService(market, inventory, [resource], buyer, seller);

        var result = await service.BuyCommodityAsync(
            buyerId,
            resource.Id,
            3,
            10,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.FilledQuantity);
        Assert.Equal(30, result.TotalPrice);
        Assert.Equal(70, buyer.Cinders);
        Assert.Equal(29, seller.Cinders);
        Assert.Same(ownListing, Assert.Single(market.Listings));
        Assert.Equal(5, ownListing.Quantity);
        Assert.Equal(3, Assert.Single(inventory.MarketPurchases).Quantity);
        Assert.Equal(sellerId, Assert.Single(market.Orders).SellerId);
    }

    [Fact]
    public async Task SellCommodity_WithInsufficientDemand_DoesNotRemoveInventory()
    {
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var inventory = new FakeInventoryService(CreateInventoryItem(resource, 10));
        var market = new FakeMarketRepository();
        market.BuyOrders.Add(new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 3,
            UnitPrice = 20,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var service = CreateService(
            market,
            inventory,
            [resource],
            new Character { Id = sellerId },
            new Character { Id = buyerId });

        var result = await service.SellCommodityAsync(
            sellerId,
            inventory.Item!.ItemInstanceId,
            5,
            15,
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, inventory.RemoveForListingCalls);
    }

    [Fact]
    public async Task CreateBuyOrder_FillsCrossingListingsAndEscrowsOnlyTheRemainder()
    {
        var buyerId = Guid.NewGuid();
        var firstSellerId = Guid.NewGuid();
        var secondSellerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var firstListedItem = CreateInventoryItem(resource, 2);
        var secondListedItem = CreateInventoryItem(resource, 1);
        var firstListing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = firstSellerId,
            ItemInstanceId = firstListedItem.ItemInstanceId,
            ItemInstance = firstListedItem.ItemInstance,
            Quantity = 2,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var secondListing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = secondSellerId,
            ItemInstanceId = secondListedItem.ItemInstanceId,
            ItemInstance = secondListedItem.ItemInstance,
            Quantity = 1,
            UnitPrice = 11,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var market = new FakeMarketRepository();
        market.Listings.Add(firstListing);
        market.Listings.Add(secondListing);
        var inventory = new FakeInventoryService(null);
        var buyer = new Character { Id = buyerId, Cinders = 100 };
        var firstSeller = new Character { Id = firstSellerId, Cinders = 0 };
        var secondSeller = new Character { Id = secondSellerId, Cinders = 0 };
        var service = CreateService(
            market,
            inventory,
            [resource],
            buyer,
            firstSeller,
            secondSeller);

        var result = await service.CreateMarketPlaceBuyOrderAsync(
            buyerId,
            new MarketPlaceBuyOrder
            {
                BuyerId = buyerId,
                ItemBaseId = resource.Id,
                Quantity = 5,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.FilledQuantity);
        Assert.Equal(31, result.FilledTotalPrice);
        Assert.Equal(45, result.BuyerCinders);
        Assert.Equal(19, firstSeller.Cinders);
        Assert.Equal(10, secondSeller.Cinders);
        Assert.Empty(market.Listings);
        var remainder = Assert.Single(market.BuyOrders);
        Assert.Same(remainder, result.BuyOrder);
        Assert.Equal(2, remainder.Quantity);
        Assert.Equal(12, remainder.UnitPrice);
        Assert.Equal(3, inventory.MarketPurchases.Sum(x => x.Quantity));
        Assert.Collection(
            market.Orders,
            trade =>
            {
                Assert.Equal(2, trade.Quantity);
                Assert.Equal(10, trade.UnitPrice);
                Assert.Equal(20, trade.TotalPrice);
            },
            trade =>
            {
                Assert.Equal(1, trade.Quantity);
                Assert.Equal(11, trade.UnitPrice);
                Assert.Equal(11, trade.TotalPrice);
            });
    }

    [Fact]
    public async Task CreateBuyOrder_WhenFullyCrossed_DoesNotConsumeABuyOrderSlot()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var listedItem = CreateInventoryItem(resource, 2);
        var market = new FakeMarketRepository { BuyOrderCount = 10 };
        market.Listings.Add(new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            ItemInstanceId = listedItem.ItemInstanceId,
            ItemInstance = listedItem.ItemInstance,
            Quantity = 2,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var inventory = new FakeInventoryService(null);
        var buyer = new Character { Id = buyerId, Cinders = 100 };
        var seller = new Character { Id = sellerId, Cinders = 0 };
        var service = CreateService(market, inventory, [resource], buyer, seller);

        var result = await service.CreateMarketPlaceBuyOrderAsync(
            buyerId,
            new MarketPlaceBuyOrder
            {
                BuyerId = buyerId,
                ItemBaseId = resource.Id,
                Quantity = 2,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.BuyOrder);
        Assert.Equal(2, result.FilledQuantity);
        Assert.Equal(20, result.FilledTotalPrice);
        Assert.Equal(80, result.BuyerCinders);
        Assert.Empty(market.BuyOrders);
        Assert.Empty(market.Listings);
    }

    [Fact]
    public async Task CreateBuyOrder_RejectsActiveSellListingForTheSameItem()
    {
        var characterId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var listedItem = CreateInventoryItem(resource, 2);
        var market = new FakeMarketRepository();
        market.Listings.Add(new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = characterId,
            ItemInstanceId = listedItem.ItemInstanceId,
            ItemInstance = listedItem.ItemInstance,
            Quantity = 2,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var buyer = new Character { Id = characterId, Cinders = 100 };
        var service = CreateService(
            market,
            new FakeInventoryService(null),
            [resource],
            buyer);

        var result = await service.CreateMarketPlaceBuyOrderAsync(
            characterId,
            new MarketPlaceBuyOrder
            {
                BuyerId = characterId,
                ItemBaseId = resource.Id,
                Quantity = 2,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(market.BuyOrders);
        Assert.Equal(100, buyer.Cinders);
    }

    [Fact]
    public async Task CreateBuyOrder_RejectsSecondActiveBuyOrderForTheSameItem()
    {
        var characterId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var existingOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = characterId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 2,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var market = new FakeMarketRepository();
        market.BuyOrders.Add(existingOrder);
        var buyer = new Character { Id = characterId, Cinders = 100 };
        var service = CreateService(
            market,
            new FakeInventoryService(null),
            [resource],
            buyer);

        var result = await service.CreateMarketPlaceBuyOrderAsync(
            characterId,
            new MarketPlaceBuyOrder
            {
                BuyerId = characterId,
                ItemBaseId = resource.Id,
                Quantity = 2,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.Null(result);
        Assert.Same(existingOrder, Assert.Single(market.BuyOrders));
        Assert.Equal(100, buyer.Cinders);
    }

    [Fact]
    public async Task CreateBuyOrder_AllowsActiveSellListingForADifferentItem()
    {
        var characterId = Guid.NewGuid();
        var ore = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var wood = new ItemBase { Id = "wood", Name = "Wood", Stackable = true };
        var listedWood = CreateInventoryItem(wood, 2);
        var market = new FakeMarketRepository();
        market.Listings.Add(new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = characterId,
            ItemInstanceId = listedWood.ItemInstanceId,
            ItemInstance = listedWood.ItemInstance,
            Quantity = 2,
            UnitPrice = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        var buyer = new Character { Id = characterId, Cinders = 100 };
        var service = CreateService(
            market,
            new FakeInventoryService(null),
            [ore, wood],
            buyer);

        var result = await service.CreateMarketPlaceBuyOrderAsync(
            characterId,
            new MarketPlaceBuyOrder
            {
                BuyerId = characterId,
                ItemBaseId = ore.Id,
                Quantity = 2,
                UnitPrice = 12
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(market.BuyOrders);
        Assert.Equal(76, buyer.Cinders);
    }

    [Fact]
    public async Task ExpireOrders_ReturnsListingItemAndRefundsRemainingBuyOrderEscrow()
    {
        var now = DateTimeOffset.UtcNow;
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var resource = new ItemBase { Id = "ore", Name = "Ore", Stackable = true };
        var listedItem = CreateInventoryItem(resource, 4);
        var listing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            ItemInstanceId = listedItem.ItemInstanceId,
            ItemInstance = listedItem.ItemInstance,
            Quantity = 4,
            UnitPrice = 10,
            ExpiresAt = now.AddMinutes(-1)
        };
        var buyOrder = new MarketPlaceBuyOrder
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            ItemBaseId = resource.Id,
            ItemBase = resource,
            Quantity = 3,
            UnitPrice = 20,
            ExpiresAt = now.AddMinutes(-1)
        };
        var market = new FakeMarketRepository();
        market.Listings.Add(listing);
        market.BuyOrders.Add(buyOrder);
        var inventory = new FakeInventoryService(null);
        var buyer = new Character { Id = buyerId, Cinders = 5 };
        var service = CreateService(market, inventory, [resource], buyer);

        var result = await service.ExpireOrdersAsync(now, 100, CancellationToken.None);

        Assert.Equal(1, result.ExpiredListings);
        Assert.Equal(1, result.ExpiredBuyOrders);
        Assert.Equal(60, result.RefundedCinders);
        Assert.Equal(65, buyer.Cinders);
        Assert.Empty(market.Listings);
        Assert.Empty(market.BuyOrders);
        Assert.Equal(4, inventory.Item?.Quantity);
    }

    private static MarketPlaceService CreateService(
        FakeMarketRepository market,
        FakeInventoryService inventory,
        IReadOnlyCollection<ItemBase> itemBases,
        params Character[] characters)
    {
        var characterService = new FakeCharacterService(characters);
        return new MarketPlaceService(
            market,
            new FakeItemBaseRepository(itemBases),
            inventory,
            characterService,
            Options.Create(new MarketPlaceOptions()),
            TimeProvider.System);
    }

    private static InventoryItem CreateInventoryItem(ItemBase itemBase, int quantity)
    {
        var instance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };
        return new InventoryItem
        {
            InventoryId = Guid.NewGuid(),
            ItemInstanceId = instance.Id,
            ItemInstance = instance,
            Quantity = quantity
        };
    }

    private sealed class FakeMarketRepository : IMarketPlaceRepository
    {
        public int ListingCount { get; set; }
        public int BuyOrderCount { get; set; }
        public List<MarketPlaceListing> Listings { get; } = [];
        public List<MarketPlaceBuyOrder> BuyOrders { get; } = [];
        public List<MarketPlaceOrder> Orders { get; } = [];

        public Task<int> GetListingCountAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(ListingCount);
        public Task<int> GetBuyOrderCountAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(BuyOrderCount);
        public Task<bool> HasActiveListingForItemAsync(Guid characterId, string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(Listings.Any(x => x.SellerId == characterId && x.ItemInstance.ItemBaseId == itemBaseId && x.ExpiresAt > now));
        public Task<bool> HasActiveBuyOrderForItemAsync(Guid characterId, string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(BuyOrders.Any(x => x.BuyerId == characterId && x.ItemBaseId == itemBaseId && x.ExpiresAt > now));
        public Task LockCharactersAsync(IReadOnlyCollection<Guid> characterIds, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<List<MarketPlaceListing>> GetMarketPlaceListingsAsync(CancellationToken cancellationToken) => Task.FromResult(Listings);
        public Task<List<MarketPlaceListing>> GetCommodityListingsAsync(string itemBaseId, long maximumUnitPrice, CancellationToken cancellationToken) =>
            Task.FromResult(Listings.Where(x => x.ItemInstance.ItemBaseId == itemBaseId && x.UnitPrice <= maximumUnitPrice).ToList());
        public Task<List<MarketPlaceBuyOrder>> GetMarketPlaceBuyOrdersAsync(CancellationToken cancellationToken) => Task.FromResult(BuyOrders);
        public Task<List<MarketPlaceBuyOrder>> GetCommodityBuyOrdersAsync(string itemBaseId, long minimumUnitPrice, CancellationToken cancellationToken) =>
            Task.FromResult(BuyOrders.Where(x => x.ItemBaseId == itemBaseId && x.UnitPrice >= minimumUnitPrice).ToList());
        public Task<List<Guid>> GetExpiredListingIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken) =>
            Task.FromResult(Listings.Where(x => x.ExpiresAt <= now).Take(take).Select(x => x.Id).ToList());
        public Task<List<Guid>> GetExpiredBuyOrderIdsAsync(DateTimeOffset now, int take, CancellationToken cancellationToken) =>
            Task.FromResult(BuyOrders.Where(x => x.ExpiresAt <= now).Take(take).Select(x => x.Id).ToList());
        public Task<MarketPlaceItemSummary> GetItemSummaryAsync(string itemBaseId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketPlaceItemSummary(itemBaseId, null, 0, null, 0, null, null, 0));
        public Task<List<MarketPlaceOrder>> GetOrderHistoryAsync(Guid characterId, int take, CancellationToken cancellationToken) => Task.FromResult(Orders);
        public Task AddOrderAsync(MarketPlaceOrder order, CancellationToken cancellationToken) { Orders.Add(order); return Task.CompletedTask; }
        public Task<MarketPlaceListing?> CreateMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken) { Listings.Add(listing); return Task.FromResult<MarketPlaceListing?>(listing); }
        public Task<MarketPlaceBuyOrder?> CreateMarketPlaceBuyOrderAsync(Guid characterId, MarketPlaceBuyOrder order, CancellationToken cancellationToken) { BuyOrders.Add(order); return Task.FromResult<MarketPlaceBuyOrder?>(order); }
        public Task<bool> BuyoutMarketPlaceListingAsync(Guid characterId, Guid listingId, int quantity, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CancelMarketPlaceListingAsync(Guid characterId, Guid listingId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<MarketPlaceListing?> GetListingAsync(Guid listingId, CancellationToken cancellationToken) => Task.FromResult(Listings.FirstOrDefault(x => x.Id == listingId));
        public Task<MarketPlaceBuyOrder?> GetBuyOrderAsync(Guid buyOrderId, CancellationToken cancellationToken) => Task.FromResult(BuyOrders.FirstOrDefault(x => x.Id == buyOrderId));
        public void RemoveListingAsync(MarketPlaceListing listing) => Listings.Remove(listing);
        public void RemoveBuyOrder(MarketPlaceBuyOrder buyOrder) => BuyOrders.Remove(buyOrder);
    }

    private sealed class FakeItemBaseRepository(IReadOnlyCollection<ItemBase> itemBases) : IItemBaseRepository
    {
        public Task<List<ItemBase>> GetTradableItemBasesAsync(CancellationToken cancellationToken) => Task.FromResult(itemBases.ToList());
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(itemBases.Where(x => itemIds.Contains(x.Id)).ToDictionary(x => x.Id));
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(string itemBaseId, CancellationToken cancellationToken) => Task.FromResult(itemBases.OfType<EquipmentBase>().FirstOrDefault(x => x.Id == itemBaseId));
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBasesToAdd, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeInventoryService(InventoryItem? item) : IInventoryService
    {
        public InventoryItem? Item { get; private set; } = item;
        public int RemoveForListingCalls { get; private set; }
        public List<InventoryItem> MarketPurchases { get; } = [];
        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult<Inventory?>(null);
        public Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) => Task.FromResult(Item?.ItemInstanceId == itemInstanceId ? Item : null);
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken) { RemoveForListingCalls++; return Task.FromResult(true); }
        public Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken)
        {
            MarketPurchases.Add(inventoryItem);
            Item = inventoryItem;
            return Task.CompletedTask;
        }
        public Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken) => Task.FromResult<InventoryItem?>(null);
    }

    private sealed class FakeCharacterService(IEnumerable<Character> characters) : ICharacterService
    {
        private readonly Dictionary<Guid, Character> _characters = characters.ToDictionary(x => x.Id);
        public Task<Character?> GetCharacterByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(_characters.GetValueOrDefault(characterId));
        public Task<Character> CreateCharacterAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterAsync(Guid currentUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetMyCharacterOverviewAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterOverviewByNameAsync(string characterName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> UpdateCharacterNameAsync(Guid userId, string username, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsCharacterNameTakenAsync(string name, Guid? excludedCharacterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetBaseCharacterByIdAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetCharacterWithSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> GetCharacterIdByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> GetCombatRatingAsync(Guid characterId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
