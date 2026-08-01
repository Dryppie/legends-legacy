using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Shop;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Services.LL.Guilds;

namespace EssenceSystem.Tests;

public sealed class GuildShopServiceTests
{
    [Fact]
    public void Default_stock_prioritizes_progression_resources_and_blueprints()
    {
        var content = new DefaultGuildContentProvider();

        var commonCatalysts = content.ShopItems
            .Where(x => x.RotationGroup == "common-catalysts")
            .ToList();
        var soulstoneReserve = content.ShopItems.Single(x => x.Key == "common.soulstone_cache");
        var fragmentCase = content.ShopItems.Single(x => x.Key == "common.sigil_fragment_case");
        var rareCatalysts = content.ShopItems
            .Where(x => x.RotationGroup == "rare-catalysts")
            .ToList();
        var blueprints = content.ShopItems
            .Where(x => x.RotationGroup == "rare-blueprints")
            .ToList();

        Assert.Equal(5, commonCatalysts.Count);
        Assert.All(commonCatalysts, item =>
        {
            Assert.Equal(GuildShopStockType.Common, item.StockType);
            Assert.Equal(100, item.GuildFavorCost);
            Assert.True(item.RotatesWeekly);
            Assert.Contains(item.Rewards, reward => reward.Type == GuildShopRewardType.Item && reward.Amount == 2);
        });
        Assert.Contains(soulstoneReserve.Rewards, x => x.Type == GuildShopRewardType.Soulstones && x.Amount == 25);
        Assert.Contains(fragmentCase.Rewards, x => x.Type == GuildShopRewardType.SigilFragments && x.Amount == 10);
        Assert.Equal(5, rareCatalysts.Count);
        Assert.All(rareCatalysts, item =>
        {
            Assert.Equal(GuildShopStockType.Rare, item.StockType);
            Assert.Equal(250, item.GuildFavorCost);
            Assert.Contains(item.Rewards, reward => reward.Type == GuildShopRewardType.Item && reward.Amount == 6);
        });
        Assert.Equal(11, blueprints.Count);
        Assert.All(blueprints, item =>
        {
            Assert.Equal(4, item.RequiredMarketOfficeLevel);
            Assert.Contains(item.Rewards, reward => reward.Type == GuildShopRewardType.Item && reward.Key!.StartsWith("blueprint_"));
        });
        Assert.DoesNotContain(
            content.ShopItems.SelectMany(item => item.Rewards),
            reward => reward.Type is GuildShopRewardType.Cinders or GuildShopRewardType.FateEcho);
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
    public async Task Level_four_offers_and_grants_one_rotating_blueprint()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now);
        await db.SaveChangesAsync();
        var service = new GuildShopService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var blueprint = Assert.Single(overview!.Items, x => x.Key.StartsWith("rare.blueprint_"));
        var reward = Assert.Single(blueprint.Rewards);
        db.ItemBases.Add(new ItemBase
        {
            Id = reward.Key!,
            Name = reward.Name!,
            ItemType = ItemType.Resource,
            Rarity = Rarity.Rare,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var result = await service.PurchaseAsync(characterId, blueprint.Key, now, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(50, result.Value!.GuildFavor);
        Assert.Contains(db.InventoryItems.Local, item =>
            item.InventoryId == characterId && item.ItemInstance.ItemBaseId == reward.Key);
    }

    [Fact]
    public async Task Common_catalyst_cache_grants_two_stackable_crafting_materials()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now);
        await db.SaveChangesAsync();
        var service = new GuildShopService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);
        var catalystCache = overview!.Items.First(x =>
            x.StockType == GuildShopStockType.Common
            && x.Key.EndsWith("_catalyst_cache"));
        var reward = Assert.Single(catalystCache.Rewards);
        db.ItemBases.Add(CreateResource(reward.Key!, reward.Name!));
        await db.SaveChangesAsync();

        var result = await service.PurchaseAsync(
            characterId,
            catalystCache.Key,
            now,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(400, result.Value!.GuildFavor);
        Assert.Contains(db.InventoryItems.Local, item =>
            item.InventoryId == characterId
            && item.ItemInstance.ItemBaseId == reward.Key
            && item.Quantity == 2);
    }

    [Fact]
    public async Task Common_stock_offers_two_of_five_rotating_catalyst_caches()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now);
        await db.SaveChangesAsync();
        var service = new GuildShopService(db);

        var rotations = new HashSet<string>();
        for (var week = 0; week < 8; week++)
        {
            var overview = await service.GetOverviewAsync(characterId, now.AddDays(7 * week), CancellationToken.None);
            var keys = overview!.Items
                .Where(x => x.StockType == GuildShopStockType.Common && x.Key.EndsWith("_catalyst_cache"))
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(2, keys.Count);
            rotations.Add(string.Join('|', keys));
        }

        Assert.True(rotations.Count > 1);
    }

    [Fact]
    public async Task Level_five_adds_a_second_rotating_rare_catalyst_cache()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        var characterId = SeedGuild(db, now, marketOfficeLevel: 5);
        await db.SaveChangesAsync();
        var service = new GuildShopService(db);

        var overview = await service.GetOverviewAsync(characterId, now, CancellationToken.None);

        Assert.Equal(2, overview!.Items.Count(x => x.Key.EndsWith("_catalyst_cache") && x.StockType == GuildShopStockType.Rare));
    }

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static ItemBase CreateResource(string id, string name) => new()
    {
        Id = id,
        Name = name,
        ItemType = ItemType.Resource,
        Rarity = Rarity.Rare,
        Stackable = true
    };

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
        db.GuildMemberContributionPeriods.Add(new GuildMemberContributionPeriod
        {
            GuildId = guildId,
            CharacterId = characterId,
            PeriodType = GuildMissionPeriodType.Weekly,
            PeriodKey = "20260727",
            ContributionScore = 400
        });

        return characterId;
    }
}
