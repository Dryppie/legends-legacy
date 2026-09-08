using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Shop;
using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Guilds;
using Persistence.LL.Repositories.Inventories;
using Services.LL.Guilds;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed partial class GuildShopServiceTests
{
    [Fact]
    public void Default_stock_contains_only_fixed_currency_supplies()
    {
        var content = new DefaultGuildContentProvider();

        Assert.Equal(4, content.ShopItems.Count);
        Assert.All(content.ShopItems, item =>
        {
            Assert.False(item.RotatesWeekly);
            Assert.Null(item.RotationGroup);
            Assert.All(item.Rewards, reward => Assert.True(
                reward.Type is GuildShopRewardType.Soulstones or GuildShopRewardType.SigilFragments));
        });
    }

    [Fact]
    public void Json_stock_matches_code_fallback()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var json = new JsonGuildContentProvider(
            new ConfigurationBuilder().Build(),
            AppContext.BaseDirectory,
            options);
        var fallback = new DefaultGuildContentProvider();

        Assert.Equal(fallback.ShopItems.Count, json.ShopItems.Count);
        foreach (var expected in fallback.ShopItems)
        {
            var actual = Assert.Single(json.ShopItems, item => item.Key == expected.Key);
            Assert.Equal(expected with { Rewards = actual.Rewards }, actual);
            Assert.Equal(expected.Rewards, actual.Rewards);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Market_office_level_unlocks_stock_without_weekly_contribution(int marketOfficeLevel)
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        SeedGuild(db, now, marketOfficeLevel: 4);
        var characterId = SeedGuild(db, now, marketOfficeLevel);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Empty(await db.GuildMemberContributionPeriods.ToListAsync());
        Assert.Equal(4, overview!.Items.Count);
        Assert.All(overview.Items, item => Assert.Equal(
            item.RequiredMarketOfficeLevel <= marketOfficeLevel,
            item.CanPurchase));
    }

    [Fact]
    public async Task Shop_overview_and_purchase_reject_a_character_without_membership()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now);
        await db.SaveChangesAsync();
        db.GuildMembers.Remove(await db.GuildMembers.SingleAsync());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);

        Assert.Null(await service.GetOverviewAsync(characterId, now, CancellationToken.None));
        var purchase = await service.PurchaseAsync(
            characterId, "common.sigil_fragment_case", now, CancellationToken.None);

        Assert.False(purchase.Succeeded);
        Assert.Equal("You are not in a guild.", purchase.Error);
        Assert.Equal(500, (await db.Characters.SingleAsync()).GuildFavor);
        Assert.Empty(db.GuildShopPurchases.Local);
        Assert.Empty(db.GuildActivityLogs.Local);
    }

    [Fact]
    public async Task Fragment_purchase_grants_a_bound_inventory_stack()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now);
        SigilFragmentTestItems.Seed(db, characterId, 5);
        await db.SaveChangesAsync();
        var result = await CreateService(db).PurchaseAsync(characterId, "common.sigil_fragment_case", now, default);
        await db.SaveChangesAsync();
        var stack = Assert.Single(await db.InventoryItems.Include(x => x.ItemInstance).ThenInclude(x => x.ItemBase).ToListAsync());
        Assert.Equal(15, stack.Quantity);
        Assert.True(stack.ItemInstance.ItemBase.IsBound);
        Assert.Equal(300, (await db.Characters.SingleAsync()).GuildFavor);
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static GuildShopService CreateService(LLDbContext db) =>
        new(
            db,
            new DefaultGuildContentProvider(),
            new InventoryItemFactory(),
            new InventoryService(new InventoryRepository(db)),
            new GuildRepository(db));

    private static Guid SeedGuild(LLDbContext db, DateTimeOffset now, int marketOfficeLevel = 4)
    {
        var characterId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "Quartermaster",
            ImagePath = "player",
            Level = 10,
            GuildFavor = 500,
            Inventory = new Inventory { CharacterId = characterId }
        });
        db.Guilds.Add(new Guild
        {
            Id = guildId,
            Name = "Shop Guild",
            OwnerId = characterId,
            Buildings =
            {
                new GuildBuilding
                {
                    GuildId = guildId,
                    Type = GuildBuildingType.MarketOffice,
                    Level = marketOfficeLevel
                }
            },
            Members =
            {
                new GuildMember
                {
                    GuildId = guildId,
                    CharacterId = characterId,
                    Role = GuildRole.Leader,
                    JoinedAt = now.AddDays(-7)
                }
            }
        });
        return characterId;
    }
}
