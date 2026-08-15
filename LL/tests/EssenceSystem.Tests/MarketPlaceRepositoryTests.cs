using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.MarketPlaces;

namespace EssenceSystem.Tests;

public sealed class MarketPlaceRepositoryTests
{
    [Fact]
    public async Task Listing_queries_load_equipment_instance_modifiers()
    {
        var databaseName = Guid.NewGuid().ToString();
        var listingId = await SeedListingAsync(databaseName);

        await using var listDb = CreateDb(databaseName);
        var listedEquipment = Assert.IsType<EquipmentInstance>(
            Assert.Single(await new MarketPlaceRepository(listDb)
                .GetMarketPlaceListingsAsync(CancellationToken.None))
                .ItemInstance);

        Assert.Equal(70, Assert.Single(listedEquipment.InstanceModifiers).Amount);

        await using var singleDb = CreateDb(databaseName);
        var singleEquipment = Assert.IsType<EquipmentInstance>(
            (await new MarketPlaceRepository(singleDb)
                .GetListingAsync(listingId, CancellationToken.None))!
                .ItemInstance);

        Assert.Equal(70, Assert.Single(singleEquipment.InstanceModifiers).Amount);
    }

    [Fact]
    public async Task AddOrder_records_account_ids_and_both_marketplace_value_movements()
    {
        await using var db = CreateDb(Guid.NewGuid().ToString());
        var sellerUser = AppUser.Register("Seller", "seller@example.com", "hash");
        var buyerUser = AppUser.Register("Buyer", "buyer@example.com", "hash");
        var seller = new Character
        {
            Id = Guid.NewGuid(),
            UserId = sellerUser.Id,
            User = sellerUser,
            Name = "Seller",
            Level = 12
        };
        var buyer = new Character
        {
            Id = Guid.NewGuid(),
            UserId = buyerUser.Id,
            User = buyerUser,
            Name = "Buyer",
            Level = 8
        };
        var itemBase = new ItemBase
        {
            Id = "market_ore",
            Name = "Market Ore",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        db.Users.AddRange(sellerUser, buyerUser);
        db.Characters.AddRange(seller, buyer);
        db.ItemBases.Add(itemBase);
        await db.SaveChangesAsync();

        var order = new MarketPlaceOrder
        {
            Id = Guid.NewGuid(),
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            ItemBaseId = itemBase.Id,
            Quantity = 4,
            UnitPrice = 25,
            TotalPrice = 100,
            SellerFee = 5,
            Source = MarketPlaceTradeSource.SellListing,
            PurchasedAt = DateTimeOffset.UtcNow
        };
        await new MarketPlaceRepository(db).AddOrderAsync(order, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal(sellerUser.Id, order.SellerAccountId);
        Assert.Equal(buyerUser.Id, order.BuyerAccountId);
        var ledger = await db.EconomyLedger.OrderBy(x => x.AssetType).ToListAsync();
        Assert.Equal(3, ledger.Count);
        Assert.Contains(ledger, x =>
            x.EventType == EconomyEventType.MarketplaceTrade &&
            x.AssetType == EconomyAssetType.Item &&
            x.SenderAccountId == sellerUser.Id &&
            x.RecipientAccountId == buyerUser.Id);
        Assert.Contains(ledger, x =>
            x.EventType == EconomyEventType.MarketplaceTrade &&
            x.AssetType == EconomyAssetType.Currency &&
            x.SenderAccountId == buyerUser.Id &&
            x.RecipientAccountId == sellerUser.Id &&
            x.Quantity == 100);
        Assert.Contains(ledger, x =>
            x.EventType == EconomyEventType.MarketplaceFee &&
            x.SenderAccountId == sellerUser.Id &&
            x.Quantity == 5);
    }

    private static async Task<Guid> SeedListingAsync(string databaseName)
    {
        await using var db = CreateDb(databaseName);
        var equipmentBase = new EquipmentBase
        {
            Id = "marketplace_relic",
            Name = "Relic",
            Description = "A test relic.",
            EquipmentType = EquipmentType.Relic
        };
        var equipment = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase,
            BaseRecipeId = "relic_recipe",
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.MaxHealth, 70)
            ]
        };
        var listing = new MarketPlaceListing
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            SellerName = "Seller",
            ItemInstanceId = equipment.Id,
            ItemInstance = equipment,
            Quantity = 1,
            UnitPrice = 1_500,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        db.MarketPlaceListings.Add(listing);
        await db.SaveChangesAsync();
        return listing.Id;
    }

    private static LLDbContext CreateDb(string databaseName)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new LLDbContext(options);
    }
}
