using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;

namespace EssenceSystem.Tests;

public sealed class InventoryRepositoryScrappingTests
{
    [Fact]
    public async Task Non_tool_equipment_can_be_scrapped_with_remaining_potential()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var weapon = AddEquipment(db, characterId, "test_weapon", EquipmentType.OneHanded, potential: 5);
        AddTemperedScrapBase(db);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.ScrapEquipments(
            characterId,
            [weapon.ItemInstanceId],
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.Quantity);
        Assert.DoesNotContain(db.InventoryItems, x => x.ItemInstanceId == weapon.ItemInstanceId);
    }

    [Fact]
    public async Task Tools_can_be_scrapped()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var tool = AddEquipment(db, characterId, "test_tool", EquipmentType.Tool, potential: null);
        AddTemperedScrapBase(db);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.ScrapEquipments(
            characterId,
            [tool.ItemInstanceId],
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.Quantity);
        Assert.DoesNotContain(db.InventoryItems, x => x.ItemInstanceId == tool.ItemInstanceId);
    }

    private static void AddTemperedScrapBase(LLDbContext db) =>
        db.ItemBases.Add(new ItemBase
        {
            Id = "tempered_scrap",
            Name = "Tempered Scrap",
            Description = "Recovered equipment material.",
            ItemType = ItemType.Resource,
            Stackable = true
        });

    private static InventoryItem AddEquipment(
        LLDbContext db,
        Guid characterId,
        string itemBaseId,
        EquipmentType equipmentType,
        int? potential)
    {
        var equipmentBase = new EquipmentBase
        {
            Id = itemBaseId,
            Name = itemBaseId,
            Description = "Test equipment.",
            EquipmentType = equipmentType
        };
        var equipment = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBaseId,
            ItemBase = equipmentBase,
            Potential = potential
        };
        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = equipment.Id,
            ItemInstance = equipment,
            Quantity = 1
        };

        db.InventoryItems.Add(inventoryItem);
        return inventoryItem;
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
