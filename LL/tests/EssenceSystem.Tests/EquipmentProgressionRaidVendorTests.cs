using System.Text.Json;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Raids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Inventories;
using Services.LL.Raids;

namespace EssenceSystem.Tests;

public sealed partial class RaidSystemTests
{
    private static RaidService VendorService(LLDbContext db, string bossId, bool progression) => new(
        db: db, definitions: new FixedRaidBossDefinitionProvider(new RaidBossDefinition { Id = bossId }),
        trophyVendor: new JsonRaidTrophyVendorCatalog(new ConfigurationBuilder().Build(), EquipmentProgressionSharedContentTests.ApiRoot(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        snapshots: null!, powerRatings: null!, inventory: new InventoryService(new InventoryRepository(db)),
        inventoryItemFactory: new InventoryItemFactory(), itemBases: new ItemBaseRepository(db),
        combatResolver: null!, playbackBundles: null!, achievements: null!, outbox: new NoopGameEventOutbox(),
        stateSync: new NoopStateSyncService(), memoryCache: new MemoryCache(new MemoryCacheOptions()), timeProvider: TimeProvider.System,
        jsonOptions: new JsonSerializerOptions(JsonSerializerDefaults.Web), options: Options.Create(new RaidOptions()),
        logger: NullLogger<RaidService>.Instance);

    [Theory]
    [InlineData("raid-boss.hives-abyss", "hive.raidforged-ingots", "raidforged_ingot", 35)]
    [InlineData("raid-boss.sanguine-horror", "sanguine.sanguine-ichor", "sanguine_ichor", 60)]
    public async Task EquipmentProgression_raid_vendor_preserves_prices_limits_and_reusable_styles(string boss, string offer, string legacyItem, int cost)
    {
        await using var db = CreateDbContext();
        var id = await SeedVendorCharacter(db, legacyItem);
        var modern = VendorService(db, boss, true);
        var preview = (await modern.GetTrophyVendorAsync(id, boss, default))!;
        var item = preview.Items.Single(x => x.Id == offer);
        Assert.Equal("tempered_scrap", item.RewardItemId);
        Assert.Equal((cost, 2, 6), (item.TrophyCost, item.RewardQuantity, item.WeeklyPurchaseLimit));
        Assert.Contains(preview.Items, x => x.Description.Contains("reusable equipment style"));
        var result = await modern.PurchaseTrophyVendorItemAsync(id, boss, offer, 2, default);
        Assert.True(result.Succeeded);
        Assert.Equal("tempered_scrap", result.Value!.RewardItemId);
        Assert.Equal(4, result.Value.RewardQuantity);
        Assert.Equal(1000 - cost * 2, (await db.Characters.SingleAsync()).RaidTrophies);
        Assert.Equal(2, (await db.RaidTrophyPurchases.SingleAsync()).Quantity);
        Assert.Equal(4, (await db.InventoryItems.SingleAsync()).Quantity);
        Assert.False((await modern.PurchaseTrophyVendorItemAsync(id, boss, offer, 5, default)).Succeeded);
        var legacy = (await VendorService(db, boss, false).GetTrophyVendorAsync(id, boss, default))!;
        Assert.Equal("tempered_scrap", legacy.Items.Single(x => x.Id == offer).RewardItemId);
        Assert.Equal(2, legacy.Items.Single(x => x.Id == offer).WeeklyPurchased);
    }

    [Fact]
    public async Task EquipmentProgression_missing_raid_vendor_reward_is_rejected_before_trophies_or_receipts_change()
    {
        await using var db = CreateDbContext();
        var id = await SeedVendorCharacter(db, "raidforged_ingot", includeScrap: false);
        var service = VendorService(db, "raid-boss.hives-abyss", true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PurchaseTrophyVendorItemAsync(id,
            "raid-boss.hives-abyss", "hive.raidforged-ingots", 1, default));
        Assert.Equal(1000, (await db.Characters.SingleAsync()).RaidTrophies);
        Assert.Empty(db.RaidTrophyPurchases.Local);
        Assert.Empty(db.InventoryItems.Local);
    }

    private static async Task<Guid> SeedVendorCharacter(LLDbContext db, string originalItem, bool includeScrap = true)
    {
        var id = Guid.NewGuid();
        db.Characters.Add(new Character { Id = id, Name = "Raider", UserId = Guid.NewGuid(), RaidTrophies = 1000,
            Inventory = new Inventory { CharacterId = id } });
        foreach (var itemId in includeScrap ? new[] { originalItem, "tempered_scrap" } : new[] { originalItem })
            db.ItemBases.Add(new ItemBase { Id = itemId, Name = itemId, Stackable = true, ItemType = ItemType.Resource });
        await db.SaveChangesAsync();
        return id;
    }
}
