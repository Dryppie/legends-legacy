using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Loadouts;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Snapshots;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentLoadoutTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Applying_current_loadout_does_not_delete_an_unpersisted_inventory_row(bool useTwoHanded)
    {
        await using var db = CreateDb();
        var (character, twoHanded, sword, _) = await Seed(db);
        var equipment = new EquipmentSlotRepository(db);
        var service = Service(db);
        var item = useTwoHanded ? twoHanded : sword;
        item.IsFavorite = true;
        (await db.InventoryItems.SingleAsync(x => x.ItemInstanceId == item.Id)).IsFavorite = true;
        Assert.True((await equipment.EquipEquipmentAsync(character.Id, item.Id, EquipmentSlotType.MainHand, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True((await service.SaveAsync(character.Id, null, "Current", default)).Succeeded);
        await db.SaveChangesAsync();
        var loadoutId = (await db.EquipmentLoadouts.SingleAsync()).Id;
        var expectedSlots = await db.EquipmentSlots.OrderBy(x => x.EquipmentSlotType)
            .Select(x => x.EquipmentInstanceId).ToListAsync();
        db.ChangeTracker.Clear();

        Assert.True((await service.ApplyAsync(character.Id, loadoutId, default)).Succeeded);
        Assert.DoesNotContain(db.ChangeTracker.Entries<InventoryItem>(), entry =>
            entry.Entity.ItemInstanceId == item.Id && entry.State == EntityState.Deleted);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(expectedSlots, await db.EquipmentSlots.OrderBy(x => x.EquipmentSlotType)
            .Select(x => x.EquipmentInstanceId).ToListAsync());
        Assert.False(await db.InventoryItems.AnyAsync(x => x.ItemInstanceId == item.Id));
        Assert.Equal(2, await db.InventoryItems.CountAsync());
        Assert.True((await db.ItemInstances.OfType<EquipmentInstance>().SingleAsync(x => x.Id == item.Id)).IsFavorite);
    }

    [Fact]
    public async Task Switching_restores_two_handed_slots_and_returns_each_displaced_item_once()
    {
        await using var db = CreateDb();
        var (character, twoHanded, sword, shield) = await Seed(db);
        var equipment = new EquipmentSlotRepository(db);
        var service = Service(db);
        Assert.True((await equipment.EquipEquipmentAsync(character.Id, twoHanded.Id, null, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True((await service.SaveAsync(character.Id, null, "Bosses", default)).Succeeded);
        await db.SaveChangesAsync();
        var loadout = Assert.Single(await service.GetAsync(character.Id, default));
        Assert.Equal(2, loadout.Slots.Count);
        Assert.True((await equipment.EquipEquipmentAsync(character.Id, sword.Id, EquipmentSlotType.MainHand, default)).Succeeded);
        Assert.True((await equipment.EquipEquipmentAsync(character.Id, shield.Id, EquipmentSlotType.OffHand, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True((await service.ApplyAsync(character.Id, loadout.Id, default)).Succeeded);
        Assert.Equal(2, character.Inventory.InventoryItems.Count);
        Assert.Equal(2, character.Inventory.InventoryItems.Select(x => x.ItemInstanceId).Distinct().Count());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var slots = await equipment.GetEquipmentSlotsByEntityIdAsync(character.Id, default);
        Assert.All(slots.Where(x => x.EquipmentSlotType is EquipmentSlotType.MainHand or EquipmentSlotType.OffHand), x => Assert.Equal(twoHanded.Id, x.EquipmentInstanceId));
        var inventoryIds = await db.InventoryItems.Select(x => x.ItemInstanceId).ToListAsync();
        Assert.Equal(2, inventoryIds.Count);
        Assert.Contains(sword.Id, inventoryIds);
        Assert.Contains(shield.Id, inventoryIds);
        Assert.DoesNotContain(twoHanded.Id, inventoryIds);
    }

    [Fact]
    public async Task Presets_enforce_limit_names_and_character_ownership()
    {
        await using var db = CreateDb();
        var (character, _, _, _) = await Seed(db);
        var service = Service(db);
        foreach (var name in new[] { "Idle", "Boss", "Arena" })
        {
            Assert.True((await service.SaveAsync(character.Id, null, name, default)).Succeeded);
            await db.SaveChangesAsync();
        }
        Assert.False((await service.SaveAsync(character.Id, null, "Fourth", default)).Succeeded);
        var first = (await service.GetAsync(character.Id, default)).First();
        Assert.False((await service.SaveAsync(character.Id, null, " idle ", default)).Succeeded);
        Assert.False((await service.DeleteAsync(Guid.NewGuid(), first.Id, default)).Succeeded);
        Assert.False((await service.ApplyAsync(Guid.NewGuid(), first.Id, default)).Succeeded);
        Assert.True((await service.SaveAsync(character.Id, first.Id, "Renamed", default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.Equal(3, await db.EquipmentLoadouts.CountAsync());
        Assert.True((await service.DeleteAsync(character.Id, first.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True((await service.SaveAsync(character.Id, null, "Replacement", default)).Succeeded);
    }

    [Fact]
    public async Task Activity_assignments_are_exclusive_and_missing_items_cannot_replace_current_equipment()
    {
        await using var db = CreateDb();
        var (character, twoHanded, sword, _) = await Seed(db);
        var equipment = new EquipmentSlotRepository(db);
        var service = Service(db);
        await equipment.EquipEquipmentAsync(character.Id, twoHanded.Id, null, default);
        await db.SaveChangesAsync();
        await service.SaveAsync(character.Id, null, "Boss", default);
        await db.SaveChangesAsync();
        var boss = Assert.Single(await service.GetAsync(character.Id, default));
        await equipment.EquipEquipmentAsync(character.Id, sword.Id, EquipmentSlotType.MainHand, default);
        await db.SaveChangesAsync();
        await service.SaveAsync(character.Id, null, "Idle", default);
        await db.SaveChangesAsync();
        var idle = (await service.GetAsync(character.Id, default)).Single(x => x.Id != boss.Id);
        await service.SetActivitiesAsync(character.Id, boss.Id, [EssenceCombatActivity.Dungeon, EssenceCombatActivity.Raid], default);
        await service.SetActivitiesAsync(character.Id, idle.Id, [EssenceCombatActivity.Raid], default);
        await db.SaveChangesAsync();
        Assert.Equal(EssenceCombatActivity.Dungeon, boss.AutoUseActivities);
        Assert.Equal(EssenceCombatActivity.Raid, idle.AutoUseActivities);
        Assert.Null(await service.ResolveAsync(character.Id, EssenceCombatActivity.None, default));
        var snapshot = await new CharacterSnapshotRepository(db, service).CreateAsync(character.Id, EssenceCombatActivity.Dungeon);
        Assert.All(snapshot.Equipment, x => Assert.Equal(twoHanded.Id, x.EquipmentInstanceId));
        Assert.Equal(sword.Id, character.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
        db.InventoryItems.Remove(await db.InventoryItems.SingleAsync(x => x.ItemInstanceId == twoHanded.Id));
        await db.SaveChangesAsync();
        Assert.False((await service.ApplyAsync(character.Id, boss.Id, default)).Succeeded);
        Assert.Null(await service.ResolveAsync(character.Id, EssenceCombatActivity.Dungeon, default));
        Assert.Equal(sword.Id, character.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
        Assert.All(snapshot.Equipment, x => Assert.Equal(twoHanded.Id, x.EquipmentInstanceId));
    }

    [Fact]
    public async Task Live_combat_uses_activity_equipment_without_replacing_frozen_snapshot_gear()
    {
        await using var db = CreateDb();
        var (character, twoHanded, sword, _) = await Seed(db);
        var equipment = new EquipmentSlotRepository(db);
        var service = Service(db);
        await equipment.EquipEquipmentAsync(character.Id, twoHanded.Id, null, default);
        await db.SaveChangesAsync();
        await service.SaveAsync(character.Id, null, "Idle", default);
        await db.SaveChangesAsync();
        var loadout = Assert.Single(await service.GetAsync(character.Id, default));
        await service.SetActivitiesAsync(character.Id, loadout.Id, [EssenceCombatActivity.IdleCombat], default);
        await equipment.EquipEquipmentAsync(character.Id, sword.Id, EquipmentSlotType.MainHand, default);
        await db.SaveChangesAsync();
        var setup = new Services.LL.Combat.CombatSetupService(null!, new EmptyEssenceResolver(), null!, null!, equipmentLoadouts: service);
        var live = setup.CreatePlayerCombatEntities([character]).Single();
        var frozen = setup.CreatePlayerCombatEntities([character]).Single();
        frozen.HasEquipmentSnapshot = true;
        await setup.PrepareEntitiesForCombat([live, frozen], EssenceCombatActivity.IdleCombat);
        Assert.Equal(twoHanded.Id, Assert.Single(live.Equipment).Id);
        Assert.Equal(twoHanded.Id, live.MainHandEquipment!.Id);
        Assert.Equal(twoHanded.Id, live.OffHandEquipment!.Id);
        Assert.Equal(sword.Id, Assert.Single(frozen.Equipment).Id);
        Assert.Equal(sword.Id, character.EquipmentSlots.Single(x => x.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
    }

    private sealed class EmptyEssenceResolver : Application.Interfaces.Services.LL.Essences.IEssenceCombatLoadoutResolver
    {
        public Task<Application.Interfaces.Services.LL.Essences.EssenceCombatLoadout> ResolveAsync(Guid id, CancellationToken ct) => Task.FromResult(Resolve(id, []));
        public Application.Interfaces.Services.LL.Essences.EssenceCombatLoadout Resolve(Guid id, IEnumerable<PlayerEssence> essences) => new(id, [], [], new HashSet<string>());
    }

    [Fact]
    public void Loadout_mapping_exposes_slots_and_individual_activity_names()
    {
        var mapper = new MapperConfiguration(c => { c.AddProfile<MappingProfile>(); c.AddProfile<EquipmentLoadoutMappingProfile>(); }, NullLoggerFactory.Instance).CreateMapper();
        var dto = mapper.Map<EquipmentLoadoutDto>(new EquipmentLoadout { Name = "Boss", AutoUseActivities = EssenceCombatActivity.Dungeon | EssenceCombatActivity.Raid });
        Assert.Equal(new[] { EssenceCombatActivity.Dungeon, EssenceCombatActivity.Raid }, dto.AutoUseActivities);
        Assert.Empty(dto.Slots);
    }

    private static LLDbContext CreateDb() => new(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static EquipmentLoadoutService Service(LLDbContext db) => new(new EquipmentLoadoutRepository(db), new EquipmentSlotRepository(db));
    private static async Task<(Character, EquipmentInstance, EquipmentInstance, EquipmentInstance)> Seed(LLDbContext db)
    {
        var character = new Character { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Loadout tester", Level = 100 };
        var twoHanded = ProgressionTestEquipment.Create(equipmentType: EquipmentType.TwoHanded, ownerId: character.Id);
        var sword = ProgressionTestEquipment.Create(equipmentType: EquipmentType.OneHanded, ownerId: character.Id);
        var shield = ProgressionTestEquipment.Create(equipmentType: EquipmentType.OffHand, ownerId: character.Id);
        character.Inventory = new Inventory { CharacterId = character.Id, Character = character };
        foreach (var item in new[] { twoHanded, sword, shield })
            character.Inventory.InventoryItems.Add(new InventoryItem { InventoryId = character.Id, ItemInstanceId = item.Id, ItemInstance = item });
        character.EquipmentSlots = Enum.GetValues<EquipmentSlotType>().Select(x => new EquipmentSlot { EntityId = character.Id, EquipmentSlotType = x }).ToList();
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        return (character, twoHanded, sword, shield);
    }
}
