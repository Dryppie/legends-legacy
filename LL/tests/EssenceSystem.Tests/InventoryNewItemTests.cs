using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;

namespace EssenceSystem.Tests;

public sealed class InventoryNewItemTests
{
    [Fact]
    public async Task Marketplace_equipment_is_new_until_the_owner_inspects_it()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        AddInventory(db, characterId);
        var item = BuildEquipment(characterId, "iron_sword", EquipmentType.OneHanded);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        await repository.AddItemsToInventory(
            characterId,
            [item],
            ItemAcquisitionSources.Marketplace,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Null(item.SeenAtUtc);
        Assert.True(item.IsNew);

        var before = DateTimeOffset.UtcNow;
        Assert.True(await repository.MarkItemSeenAsync(
            characterId,
            item.ItemInstanceId,
            CancellationToken.None));
        await db.SaveChangesAsync();

        var stored = await LoadAsync(db, characterId, item.ItemInstanceId);
        Assert.NotNull(stored.SeenAtUtc);
        Assert.InRange(stored.SeenAtUtc!.Value, before, DateTimeOffset.UtcNow);
        Assert.False(stored.IsNew);
    }

    [Theory]
    [InlineData(ItemAcquisitionSources.CombatReward, EquipmentType.OneHanded, false)]
    [InlineData(ItemAcquisitionSources.Marketplace, EquipmentType.OneHanded, true)]
    [InlineData(ItemAcquisitionSources.DungeonReward, EquipmentType.OneHanded, false)]
    [InlineData(ItemAcquisitionSources.QuestReward, EquipmentType.Head, false)]
    public void Only_eligible_equipment_acquisitions_are_new(
        string acquisitionSource,
        EquipmentType equipmentType,
        bool expected)
    {
        var item = BuildEquipment(Guid.NewGuid(), "eligibility_test", equipmentType);
        item.ItemInstance.AcquisitionSource = acquisitionSource;

        Assert.Equal(expected, item.IsNew);
    }

    [Fact]
    public void Non_equipment_is_not_new()
    {
        var item = BuildItem(Guid.NewGuid(), "crafted_resource");
        item.ItemInstance.AcquisitionSource = ItemAcquisitionSources.CombatReward;

        Assert.False(item.IsNew);
    }

    [Fact]
    public async Task Items_from_other_acquisition_sources_are_never_new()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        AddInventory(db, characterId);
        var item = BuildItem(characterId, "quest_token");
        await db.SaveChangesAsync();

        await new InventoryRepository(db).AddItemsToInventory(
            characterId,
            [item],
            ItemAcquisitionSources.QuestReward,
            CancellationToken.None);
        await db.SaveChangesAsync();

        // Unseen, but not an eligible equipment acquisition, so it must not carry the marker.
        Assert.Null(item.SeenAtUtc);
        Assert.False(item.IsNew);
    }

    [Fact]
    public async Task Marking_an_item_seen_twice_keeps_the_first_timestamp()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        AddInventory(db, characterId);
        var item = BuildItem(characterId, "iron_sword");
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        await repository.AddItemsToInventory(
            characterId,
            [item],
            ItemAcquisitionSources.CombatReward,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(await repository.MarkItemSeenAsync(
            characterId,
            item.ItemInstanceId,
            CancellationToken.None));
        await db.SaveChangesAsync();
        var firstSeenAt = (await LoadAsync(db, characterId, item.ItemInstanceId)).SeenAtUtc;

        Assert.True(await repository.MarkItemSeenAsync(
            characterId,
            item.ItemInstanceId,
            CancellationToken.None));
        await db.SaveChangesAsync();

        Assert.Equal(firstSeenAt, (await LoadAsync(db, characterId, item.ItemInstanceId)).SeenAtUtc);
    }

    [Fact]
    public async Task Marking_an_item_owned_by_another_character_does_nothing()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        AddInventory(db, ownerId, strangerId);
        var item = BuildEquipment(ownerId, "iron_sword", EquipmentType.OneHanded);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        await repository.AddItemsToInventory(
            ownerId,
            [item],
            ItemAcquisitionSources.Marketplace,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(await repository.MarkItemSeenAsync(
            strangerId,
            item.ItemInstanceId,
            CancellationToken.None));
        await db.SaveChangesAsync();

        var stored = await LoadAsync(db, ownerId, item.ItemInstanceId);
        Assert.Null(stored.SeenAtUtc);
        Assert.True(stored.IsNew);
    }

    [Fact]
    public async Task Favorite_preference_can_be_set_and_cleared_for_an_owned_item()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        AddInventory(db, characterId);
        var item = BuildItem(characterId, "favorite_sword");
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        await repository.AddItemsToInventory(
            characterId,
            [item],
            ItemAcquisitionSources.CombatReward,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(await repository.SetItemFavoriteAsync(
            characterId,
            item.ItemInstanceId,
            true,
            CancellationToken.None));
        await db.SaveChangesAsync();
        Assert.True((await LoadAsync(db, characterId, item.ItemInstanceId)).IsFavorite);

        Assert.True(await repository.SetItemFavoriteAsync(
            characterId,
            item.ItemInstanceId,
            false,
            CancellationToken.None));
        await db.SaveChangesAsync();
        Assert.False((await LoadAsync(db, characterId, item.ItemInstanceId)).IsFavorite);
    }

    [Fact]
    public async Task Favorite_preference_cannot_be_changed_by_another_character()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        AddInventory(db, ownerId, strangerId);
        var item = BuildItem(ownerId, "favorite_sword");
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        await repository.AddItemsToInventory(
            ownerId,
            [item],
            ItemAcquisitionSources.CombatReward,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.False(await repository.SetItemFavoriteAsync(
            strangerId,
            item.ItemInstanceId,
            true,
            CancellationToken.None));
        await db.SaveChangesAsync();

        Assert.False((await LoadAsync(db, ownerId, item.ItemInstanceId)).IsFavorite);
    }

    private static async Task<InventoryItem> LoadAsync(
        LLDbContext db,
        Guid characterId,
        Guid itemInstanceId) =>
        await db.InventoryItems
            .Include(x => x.ItemInstance)
            .SingleAsync(x => x.InventoryId == characterId && x.ItemInstanceId == itemInstanceId);

    private static InventoryItem BuildItem(Guid ownerId, string itemBaseId)
    {
        var itemBase = new ItemBase
        {
            Id = itemBaseId,
            Name = itemBaseId,
            Description = "New-marker test item.",
            ItemType = ItemType.Resource,
            Stackable = false
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };

        return new InventoryItem
        {
            InventoryId = ownerId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = 1
        };
    }

    private static InventoryItem BuildEquipment(
        Guid ownerId,
        string itemBaseId,
        EquipmentType equipmentType)
    {
        var itemBase = new EquipmentBase
        {
            Id = itemBaseId,
            Name = itemBaseId,
            Description = "New-marker test equipment.",
            EquipmentType = equipmentType
        };
        var itemInstance = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };

        return new InventoryItem
        {
            InventoryId = ownerId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = 1
        };
    }

    private static void AddInventory(LLDbContext db, params Guid[] characterIds)
    {
        db.Characters.AddRange(characterIds.Select((id, index) => new Character
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Name = $"NewItemTester{index}-{id:N}"[..26]
        }));
        db.Inventories.AddRange(characterIds.Select(id => new Inventory { CharacterId = id }));
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
