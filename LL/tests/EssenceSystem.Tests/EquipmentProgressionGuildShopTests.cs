using Domain.Models.Guilds.Shop;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Services.LL.Guilds;
using Services.LL.Inventories;

namespace EssenceSystem.Tests;

public sealed partial class GuildShopServiceTests
{
    private static GuildShopService EquipmentProgressionShop(LLDbContext db) => new(db, new DefaultGuildContentProvider(),
        new InventoryItemFactory(), new InventoryService(new InventoryRepository(db)));

    [Fact]
    public async Task EquipmentProgression_shop_honors_existing_purchase_count_and_delivers_previewed_scrap_once()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var characterId = SeedGuild(db, now);
        db.ItemBases.Add(CreateResource("tempered_scrap", "Tempered Scrap"));
        await db.SaveChangesAsync();
        var service = EquipmentProgressionShop(db);
        var overview = (await service.GetOverviewAsync(characterId, now, default))!;
        var item = overview.Items.First(x => x.StockType == GuildShopStockType.Common && x.Key.Contains(".scrap_cache_"));
        db.GuildShopPurchases.Add(new GuildShopPurchase { GuildId = overview.GuildId, CharacterId = characterId,
            ShopItemKey = item.Key, PeriodKey = overview.WeeklyPeriodKey, Quantity = item.WeeklyLimit - 1, PurchasedAt = now });
        await db.SaveChangesAsync();
        var purchase = await service.PurchaseAsync(characterId, item.Key, now, default);
        Assert.True(purchase.Succeeded);
        await db.SaveChangesAsync();
        Assert.False((await service.PurchaseAsync(characterId, item.Key, now, default)).Succeeded);
        Assert.Equal(500 - item.GuildFavorCost, (await db.Characters.SingleAsync()).GuildFavor);
        Assert.Equal(item.WeeklyLimit, (await db.GuildShopPurchases.SingleAsync()).Quantity);
        var loot = await db.InventoryItems.Include(x => x.ItemInstance).SingleAsync();
        Assert.Equal("tempered_scrap", loot.ItemInstance.ItemBaseId);
        Assert.Equal(Assert.Single(item.Rewards).Amount, loot.Quantity);
    }

    [Fact]
    public async Task EquipmentProgression_shop_missing_scrap_definition_does_not_charge_or_record_purchase()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var id = SeedGuild(db, now);
        await db.SaveChangesAsync();
        var service = EquipmentProgressionShop(db);
        var item = (await service.GetOverviewAsync(id, now, default))!.Items.First(x => x.Key.Contains(".scrap_cache_"));
        Assert.False((await service.PurchaseAsync(id, item.Key, now, default)).Succeeded);
        Assert.Equal(500, (await db.Characters.SingleAsync()).GuildFavor);
        Assert.Empty(db.GuildShopPurchases.Local);
        Assert.Empty(db.InventoryItems.Local);
    }
}
