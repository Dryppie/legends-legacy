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

    [Fact]
    public async Task Market_office_level_unlocks_stock_without_weekly_contribution()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now, marketOfficeLevel: 4);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Empty(await db.GuildMemberContributionPeriods.ToListAsync());
        Assert.Equal(4, overview!.Items.Count);
        Assert.All(overview.Items, item => Assert.True(item.CanPurchase));
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
            new InventoryService(new InventoryRepository(db)));

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
