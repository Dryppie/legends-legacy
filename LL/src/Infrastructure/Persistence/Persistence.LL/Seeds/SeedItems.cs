using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.LootTables;
using Domain.Models.Masteries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.LL.Seeds;
public static class SeedItems
{
    public const string AXE_GUID = "00000000-0000-0000-0001-000000000001";
    public const string BOW_GUID = "00000000-0000-0000-0001-000000000002";
    public const string DAGGER_GUID = "00000000-0000-0000-0001-000000000003";
    public const string HAMMER_GUID = "00000000-0000-0000-0001-000000000004";
    public const string SHIELD_GUID = "00000000-0000-0000-0001-000000000005";
    public const string STAFF_GUID = "00000000-0000-0000-0001-000000000006";
    public const string SWORD_GUID = "00000000-0000-0000-0001-000000000007";

    public static async Task SeedItemsData(this LLDbContext context)
    {
        await SeedstarterGear(context);
    }

    public static async Task SeedstarterGear(LLDbContext context)
    {
        // Create Items
        var axeAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Strength, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(AXE_GUID) },
        };
        var axe = new EquipmentBase
        {
            Id = Guid.Parse(AXE_GUID),
            IconPath = "iron_axe.png",
            Name = "Iron Axe",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = axeAttributes,
            CombatMastery = CombatMastery.Axe
        };
        var daggerAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Dexterity, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(DAGGER_GUID) },
        };
        var dagger = new EquipmentBase
        {
            Id = Guid.Parse(DAGGER_GUID),
            IconPath = "iron_dagger.png",
            Name = "Iron Dagger",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = daggerAttributes,
            CombatMastery = CombatMastery.Dagger
        };
        var hammerAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Endurance, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(HAMMER_GUID) },
        };
        var hammer = new EquipmentBase
        {
            Id = Guid.Parse(HAMMER_GUID),
            IconPath = "iron_hammer.png",
            Name = "Iron Hammer",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = hammerAttributes,
            CombatMastery = CombatMastery.Hammer
        };
        var swordAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.FightingSpirit, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(SWORD_GUID) } ,
        };
        var sword = new EquipmentBase
        {
            Id = Guid.Parse(SWORD_GUID),
            IconPath = "iron_sword.png",
            Name = "Iron Sword",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = swordAttributes,
            CombatMastery = CombatMastery.Sword
        };
        var bowAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Agility, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(BOW_GUID) } ,
        };
        var bow = new EquipmentBase
        {
            Id = Guid.Parse(BOW_GUID),
            IconPath = "bow.png",
            Name = "Bow",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = bowAttributes,
            CombatMastery = CombatMastery.Bow
        };
        var shieldAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Constitution, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(SHIELD_GUID) } ,
        };
        var shield = new EquipmentBase
        {
            Id = Guid.Parse(SHIELD_GUID),
            IconPath = "shield.png",
            Name = "Shield",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.OffHand,
            AttributeModifiers = shieldAttributes,
            CombatMastery = CombatMastery.Shield
        };
        var staffAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Intelligence, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse(STAFF_GUID) } ,
        };
        var staff = new EquipmentBase
        {
            Id = Guid.Parse(STAFF_GUID),
            IconPath = "staff.png",
            Name = "Staff",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = staffAttributes,
            CombatMastery = CombatMastery.Staff
        };

        var potion = new ItemBase
        {
            Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Potion"
        };

        var swordLTI = new LootTableItem
        {
            ItemId = sword.Id,
        };
        var axeLTI = new LootTableItem
        {
            ItemId = axe.Id,
        };
        var daggerLTI = new LootTableItem
        {
            ItemId = dagger.Id,
        };
        var hammerLTI = new LootTableItem
        {
            ItemId = hammer.Id,
        };
        var shieldLTI = new LootTableItem
        {
            ItemId = shield.Id,
        };
        var staffLTI = new LootTableItem
        {
            ItemId = staff.Id,
        };

        var bowLTI = new LootTableItem
        {
            ItemId = bow.Id,
        };

        var potionLTI = new LootTableItem
        {
            ItemId = potion.Id,
        };


        await context.ItemBases.AddRangeAsync(axe, dagger, hammer, sword, bow, shield, staff, potion);

        await context.LootTableItems.AddRangeAsync(swordLTI, bowLTI, axeLTI, daggerLTI, hammerLTI, shieldLTI, staffLTI, potionLTI);

        // Create LootTable and associate items with it
        var lootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [swordLTI, bowLTI, potionLTI]
        };

        await context.LootTables.AddAsync(lootTable);
    }
}
