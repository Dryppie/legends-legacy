using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Crafting;
using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Prophecies;

namespace EssenceSystem.Tests;

public sealed partial class ProphecyLifecycleTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task EquipmentProgression_cache_replacement_preserves_roll_count_currency_and_consumes_one_owned_cache(bool progression, bool scrapExists)
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var character = new Character { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Oracle" };
        var cache = new ItemBase { Id = "revelation_cache_perfect_week", Name = "Perfect Week", Stackable = true, ItemType = ItemType.Resource };
        db.Characters.Add(character);
        db.ItemBases.Add(cache);
        if (scrapExists) db.ItemBases.Add(new ItemBase { Id = "tempered_scrap", Name = "Tempered Scrap", Stackable = true, ItemType = ItemType.Resource });
        db.Inventories.Add(new Inventory { CharacterId = character.Id });
        db.InventoryItems.Add(new InventoryItem { InventoryId = character.Id, Quantity = 1,
            ItemInstance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = cache.Id, ItemBase = cache } });
        await db.SaveChangesAsync();
        var writer = new SharedRewardWriter();
        var balance = new SingleCacheBalance();
        var service = new ProphecyService(new DefinitionProvider([]), balance, new ProphecyRewardResolver(balance),
            new ExperienceProgressionProvider(), null!, new CharacterService(character), new EntityService(), null!,
            writer, new InventoryRepository(db), new ItemBaseRepository(db));
        if (!scrapExists)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenCacheAsync(character.Id, cache.Id, CancellationToken.None));
            Assert.Equal(EntityState.Unchanged, db.Entry(db.InventoryItems.Single()).State);
            Assert.Equal(1, db.InventoryItems.Single().Quantity);
            Assert.Empty(writer.Items);
            Assert.Equal(0, character.Soulstones);
            return;
        }
        var result = await service.OpenCacheAsync(character.Id, cache.Id, CancellationToken.None);
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(12, result.Value!.Reward.Soulstones);
        Assert.Equal(12, character.Soulstones);
        var item = Assert.Single(writer.Items);
        Assert.Equal(progression ? "tempered_scrap" : "tempered_scrap", item.ItemInstance.ItemBaseId);
        Assert.Equal(progression ? 8 : 4, item.Quantity);
        Assert.Equal(item.Quantity, result.Value.Reward.Items.Sum(x => x.Quantity));
        var preview = result.Value.Caches.Single().PossibleRewards;
        Assert.Contains(progression ? "Tempered Scrap" : "Tempered Scrap", preview);
        await db.SaveChangesAsync(); // Simulate the enclosing command's committed unit of work.
        Assert.Empty(db.InventoryItems);
        Assert.False((await service.OpenCacheAsync(character.Id, cache.Id, CancellationToken.None)).Succeeded);
        Assert.Single(writer.Items);
        Assert.Equal(12, character.Soulstones);
        Assert.Equal("tempered_scrap", balance.GetCatalog().Caches.Single().Rewards.Single().Reward.Items.Single().ItemId);
    }

    private sealed class SingleCacheBalance : IProphecyBalanceProvider
    {
        private readonly ProphecyBalanceCatalog _catalog = new()
        {
            Caches = [new() { ItemId = "revelation_cache_perfect_week", Title = "Perfect Week", Rolls = 4,
                PreviewRewards = ["Soulstones", "Tempered Scrap"],
                Rewards = [new() { Weight = 1, Reward = new() { Soulstones = 3,
                    Items = [new() { ItemId = "tempered_scrap", Quantity = 2 }] } }] }]
        };
        public ProphecyBalanceCatalog GetCatalog() => _catalog;
    }

    private sealed class SharedRewardWriter : IInventoryService
    {
        public List<InventoryItem> Items { get; } = [];
        public Task AddItemsToInventory(Guid id, List<InventoryItem> items, string source, CancellationToken ct) { Items.AddRange(items); return Task.CompletedTask; }
        public Task<Inventory?> GetInventoryByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task CreateInventoryAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid id, List<Material> items, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryConsumeInventoryItemAsync(Guid id, Guid item, CancellationToken ct) => throw new NotSupportedException();
        public Task<InventoryItem?> GetInventoryItemAsync(Guid id, Guid item, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> MarkItemSeenAsync(Guid id, Guid item, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> SetItemFavoriteAsync(Guid id, Guid item, bool favorite, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid id, MarketPlaceListing listing, CancellationToken ct) => throw new NotSupportedException();
        public Task<InventoryItem?> AddItemInstanceBackToInventory(Guid id, ItemInstance item, CancellationToken ct) => throw new NotSupportedException();
        public Task AddItemToInventoryFromMarketPlace(Guid id, InventoryItem item, CancellationToken ct) => throw new NotSupportedException();
        public Task<InventoryItem?> ScrapEquipments(Guid id, List<Guid> items, CancellationToken ct) => throw new NotSupportedException();
        public Task<InventoryTransferResult> TransferItemAsync(Guid sender, Guid recipient, Guid item, int quantity, CancellationToken ct) => throw new NotSupportedException();
    }
}
