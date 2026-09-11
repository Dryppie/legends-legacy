using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Domain.Models.Nobility;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.MarketPlaces;
using Persistence.LL.Repositories.Nobility;
using Services.LL.Inventories;
using Services.LL.MarketPlaces;
using Services.LL.Nobility;

namespace EssenceSystem.Tests;

public sealed partial class MarketPlaceServiceTests
{
    [Fact]
    public async Task Signets_survive_listing_partial_purchase_cancellation_and_resale_against_a_buy_order()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sellerUser = AppUser.Register("seller", "seller@example.com", "hash");
        var buyerUser = AppUser.Register("buyer", "buyer@example.com", "hash");
        var seller = new Character { Id = Guid.NewGuid(), UserId = sellerUser.Id, User = sellerUser, Name = "seller", Cinders = 1000 };
        var buyer = new Character { Id = Guid.NewGuid(), UserId = buyerUser.Id, User = buyerUser, Name = "buyer", Cinders = 1000 };
        db.Characters.AddRange(seller, buyer);
        db.ItemBases.Add(new MiscItemBase { Id = "signet", Name = "Signet", Stackable = true });
        await db.SaveChangesAsync();
        var repository = new NobilityRepository(db);
        var nobility = new NobilityService(repository, TimeProvider.System);
        var grant = await nobility.GrantAlphaAsync("operator", seller.Id, Guid.NewGuid(), 4, "Alpha trading test", default);
        Assert.True(grant.IsSuccess);
        await db.SaveChangesAsync();
        var market = new MarketPlaceService(new MarketPlaceRepository(db), new ItemBaseRepository(db),
            new InventoryService(new InventoryRepository(db)), new FakeCharacterService([seller, buyer]),
            Options.Create(new MarketPlaceOptions()), TimeProvider.System, nobility: nobility,
            signets: new SignetTradingService(repository, TimeProvider.System));
        var sellerStack = await db.InventoryItems.SingleAsync(x => x.InventoryId == seller.Id);
        var listing = new MarketPlaceListing { ItemInstanceId = sellerStack.ItemInstanceId, Quantity = 3, UnitPrice = 100 };
        Assert.NotNull(await market.CreateMarketPlaceListingAsync(seller.Id, listing, default));
        await db.SaveChangesAsync();
        Assert.Single(await repository.GetUnitsAsync(seller.Id, SignetState.Available, default));
        Assert.Equal(3, (await repository.GetUnitsAsync(seller.Id, SignetState.Listed, default)).Count);

        Assert.NotNull(await market.BuyoutMarketPlaceListingAsync(buyer.Id, listing.Id, 1, default));
        await db.SaveChangesAsync();
        Assert.Single(await repository.GetUnitsAsync(buyer.Id, SignetState.Available, default));
        Assert.Equal(900, buyer.Cinders);
        Assert.NotNull(await market.CancelMarketPlaceListingAsync(seller.Id, listing.Id, default));
        await db.SaveChangesAsync();
        Assert.Equal(3, (await db.InventoryItems.SingleAsync(x => x.InventoryId == seller.Id)).Quantity);
        Assert.Empty(await repository.GetUnitsAsync(seller.Id, SignetState.Listed, default));

        Assert.NotNull(await market.CreateMarketPlaceBuyOrderAsync(seller.Id,
            new MarketPlaceBuyOrder { ItemBaseId = "signet", Quantity = 1, UnitPrice = 100 }, default));
        await db.SaveChangesAsync();
        var buyerStack = await db.InventoryItems.SingleAsync(x => x.InventoryId == buyer.Id);
        Assert.NotNull(await market.CreateMarketPlaceListingAsync(buyer.Id,
            new MarketPlaceListing { ItemInstanceId = buyerStack.ItemInstanceId, Quantity = 1, UnitPrice = 100 }, default));
        await db.SaveChangesAsync();
        Assert.Equal(4, (await db.InventoryItems.SingleAsync(x => x.InventoryId == seller.Id)).Quantity);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.InventoryId == buyer.Id));
        Assert.Empty(await db.MarketPlaceListings.ToListAsync());
        Assert.Empty(await db.MarketPlaceBuyOrders.ToListAsync());
        Assert.Equal(2, await db.MarketPlaceOrders.CountAsync());
        Assert.All(await db.Set<SignetUnit>().ToListAsync(), unit =>
        { Assert.Equal(seller.Id, unit.OwnerCharacterId); Assert.Equal(grant.Data!.Id, unit.IssuanceId); Assert.Equal(SignetState.Available, unit.State); });
        Assert.False(await repository.HasCashSupportAsync(buyer.UserId, default));
    }
}
