using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class EquipmentSnapshotPersistenceTests
{
    [Fact]
    public void Display_name_prefers_the_persisted_instance_name_for_tools()
    {
        var equipment = new EquipmentInstance
        {
            CraftedName = "Broken Proven Pickaxe",
            Rarity = Rarity.Rare,
            ItemBase = new EquipmentBase
            {
                Name = "Pickaxe",
                EquipmentType = EquipmentType.Tool
            }
        };

        Assert.Equal("Broken Proven Pickaxe", equipment.DisplayName);

        equipment.CraftedName = null;

        Assert.Equal("Proven Pickaxe", equipment.DisplayName);
    }

    [Fact]
    public async Task Saving_equipment_snapshot_does_not_add_modifiers_to_live_equipment()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var equipmentId = Guid.NewGuid();
        var equipment = new EquipmentInstance
        {
            Id = equipmentId,
            ItemBaseId = "test.snapshot.helm",
            BaseRecipeId = "recipe.test.snapshot.helm",
            BlueprintId = "blueprint.test.snapshot.helm",
            CraftedName = "Broken Snapshot Helm",
            ItemBase = new EquipmentBase
            {
                Id = "test.snapshot.helm",
                Name = "Snapshot Helm",
                EquipmentType = EquipmentType.Head
            },
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.MaxHealth, 11)
                {
                    Id = Guid.NewGuid(),
                    ItemInstanceId = equipmentId
                }
            ]
        };
        var characterSnapshot = new CharacterSnapshot
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            Name = "Snapshot Character",
            Level = 1,
            Equipment = [EquipmentSnapshot.From(EquipmentSlotType.Head, equipment)]
        };

        await using (var writeContext = new LLDbContext(options))
        {
            writeContext.ItemInstances.Add(equipment);
            writeContext.CharacterSnapshots.Add(characterSnapshot);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new LLDbContext(options);
        var persistedEquipment = await readContext.ItemInstances
            .OfType<EquipmentInstance>()
            .Include(x => x.ItemBase)
            .Include(x => x.InstanceModifiers)
            .SingleAsync(x => x.Id == equipmentId);
        var persistedSnapshot = await readContext.CharacterSnapshots
            .Include(x => x.Equipment)
                .ThenInclude(x => x.InstanceModifiers)
            .SingleAsync(x => x.Id == characterSnapshot.Id);

        var liveModifier = Assert.Single(persistedEquipment.InstanceModifiers);
        Assert.Equal(AttributeType.MaxHealth, liveModifier.AttributeType);
        Assert.Equal(11, liveModifier.Amount);

        var snapshotModifier = Assert.Single(
            Assert.Single(persistedSnapshot.Equipment).InstanceModifiers);
        Assert.Equal(
            "recipe.test.snapshot.helm",
            Assert.Single(persistedSnapshot.Equipment).BaseRecipeId);
        Assert.Equal(
            "blueprint.test.snapshot.helm",
            Assert.Single(persistedSnapshot.Equipment).BlueprintId);
        Assert.Equal("Broken Snapshot Helm", persistedEquipment.DisplayName);
        Assert.Equal(AttributeType.MaxHealth, snapshotModifier.AttributeType);
        Assert.Equal(11, snapshotModifier.Amount);
    }
}
