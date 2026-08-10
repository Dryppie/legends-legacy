using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.MarketPlaces;
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
