using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.GatheringNodes;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.Masteries;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Persistence.LL.Repositories.Users;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";

    public static async Task SeedData(this LLDbContext context, IPasswordHasher<AppUser> hasher)
    {
        if (!context.Entities.Any())
        {
            await SeedCreatures.SeedCreaturesData(context);
            await SeedItems.SeedItemsData(context);
            await SeedItemsAndLootTables(context);
            await SeedAdminData(context, hasher);

            await SeedInventoryItems(context);

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAdminData(LLDbContext context, IPasswordHasher<AppUser> hasher)
    {
        var email = "admin@hotmail.com";
        var user = new AppUser
        {
            Username = "admin",
            Email = email,
            PasswordHash = hasher.HashPassword(null!, "Password123!"),
            EmailConfirmed = true,
            IsGuest = false,
        };

        await context.Users.AddAsync(user);

        var character = new Character()
        {
            Id = Guid.Parse(CHARACTER_GUID),
            UserId = user.Id,
            Name = "admin",
            ImagePath = "player",
            Level = 1
        };

        var inventory = new Inventory()
        {
            CharacterId = character.Id,
        };

        var attributes = EntityBaseAttributeHelper.CreateEntityAttributes(character.Id);
        await context.EntityAttributes.AddRangeAsync(attributes);

        //var abilities = new List<AbilityId>()
        //{
        //    new AbilityId()
        //    {
        //        EntityId = character.Id,
        //        Id = "fireball_01"
        //    },
        //    new AbilityId()
        //    {
        //        EntityId = character.Id,
        //        Id = "_01"
        //    }
        //};
        var essences = new List<Essence>()
        {
            new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Starter Essence 1",
                ActiveAbilityId = "fireball_01",
                PassiveAbilityId = "retaliate_01"
            },
            new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Starter Essence 2",
                ActiveAbilityId = "heal_01",
                PassiveAbilityId = "pocketDirt"
            }
        };

        var essenceSlots = new List<EssenceSlot>()
        {
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = essences.First(),
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = essences.Last(),
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
            new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                EntityId = character.Id,
            },
        };

        var arenaTicketStatus = new ArenaTicketStatus()
        {
            CharacterId = character.Id,
            CurrentTickets = 5,
            LastTicketUpdate = DateTime.UtcNow,
        };
        character.ArenaTicketStatus = arenaTicketStatus;

        var equipmentSlots = SeedEquipmentSlots(character);
        var masteries = SeedMasteries(character);
        context.EquipmentSlots.AddRange(equipmentSlots);
        character.EssenceSlots = essenceSlots;
        character.Masteries = masteries;
        context.Characters.Add(character);
        context.Inventories.Add(inventory);
        await context.Essences.AddRangeAsync(essences);
    }
    private static List<Mastery> SeedMasteries(Entity entity)
    {
        var masteries = new List<Mastery>()
        {
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Axe,
                AttributeType = AttributeType.Strength,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Bow,
                AttributeType = AttributeType.Agility,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Dagger,
                AttributeType = AttributeType.Dexterity,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Hammer,
                AttributeType = AttributeType.Endurance,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Shield,
                AttributeType = AttributeType.Constitution,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Staff,
                AttributeType = AttributeType.Intelligence,
            },
            new Mastery()
            {
                EntityId = entity.Id,
                Level = 0,
                CurrentXP = 0,
                MasteryType = CombatMastery.Sword,
                AttributeType = AttributeType.FightingSpirit,
            },
        };

        return masteries;
    }

    private static List<EquipmentSlot> SeedEquipmentSlots(Entity entity)
    {

        var slotTypes = Enum.GetValues(typeof(EquipmentType)).Cast<EquipmentType>();

        // Create an equipment slot for each enum value
        var equipmentSlots = slotTypes
            .Select(type => new EquipmentSlot
            {
                EntityId = entity.Id,
                EquipmentType = type
            })
            .ToList();

        return equipmentSlots;
    }

    public static async Task SeedItemsAndLootTables(LLDbContext context)
    {
        await SeedWoodcuttingLootTables(context);
    }

    public static async Task SeedInventoryItems(LLDbContext context)
    {
        if (!context.InventoryItems.Any())
        {
            var goblinEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ItemBaseId = "00000000-0000-0000-0000-000000000001",
            };
            var ratEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                ItemBaseId = "00000000-0000-0000-0000-000000000004",
            };
            var swordEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.SWORD_GUID),
                ItemBaseId = SeedItems.SWORD_GUID,
            };
            var bowEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.BOW_GUID),
                ItemBaseId = SeedItems.BOW_GUID,
            };
            var axeEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.AXE_GUID),
                ItemBaseId = SeedItems.AXE_GUID,
            };
            var daggerEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.DAGGER_GUID),
                ItemBaseId = SeedItems.DAGGER_GUID,
            };
            var hammerEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.HAMMER_GUID),
                ItemBaseId = SeedItems.HAMMER_GUID,
            };
            var shieldEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.SHIELD_GUID),
                ItemBaseId = SeedItems.SHIELD_GUID,
            };
            var staffEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.STAFF_GUID),
                ItemBaseId = SeedItems.STAFF_GUID,
            };
            var inventoryItemGoblinEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Copied directly from GoblinEssenceItem. Same ID
                Quantity = 1
            };

            var inventoryItemRatEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse("00000000-0000-0000-0000-000000000004"), // Copied directly from LargeRatEssenceItem. Same ID
                Quantity = 1
            };

            var inventoryItemSword = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.SWORD_GUID), // Copied directly from SwordItem. Same ID
                Quantity = 1
            };
            var inventoryItemBow = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.BOW_GUID), // Copied directly from BowItem. Same ID
                Quantity = 1
            };
            var inventoryItemAxe = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.AXE_GUID), // Copied directly from AxeItem. Same ID
                Quantity = 1
            };
            var inventoryItemDagger = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.DAGGER_GUID), // Copied directly from DaggerItem. Same ID
                Quantity = 1
            };
            var inventoryItemHammer = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.HAMMER_GUID), // Copied directly from HammerItem. Same ID
                Quantity = 1
            };
            var inventoryItemShield = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.SHIELD_GUID), // Copied directly from ShieldItem. Same ID
                Quantity = 1
            };
            var inventoryItemStaff = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemInstanceId = Guid.Parse(SeedItems.STAFF_GUID), // Copied directly from StaffItem. Same ID
                Quantity = 1
            };
            await context.ItemInstances.AddRangeAsync(goblinEssenceItemInstance, ratEssenceItemInstance, swordEquipmentInstance, bowEquipmentInstance, axeEquipmentInstance, daggerEquipmentInstance, hammerEquipmentInstance, shieldEquipmentInstance, staffEquipmentInstance);
            await context.InventoryItems.AddRangeAsync(inventoryItemGoblinEssence, inventoryItemRatEssence, inventoryItemSword, inventoryItemBow, inventoryItemAxe, inventoryItemDagger, inventoryItemHammer, inventoryItemShield, inventoryItemStaff);
        }
    }

    public static async Task SeedOddGear(LLDbContext context)
    {
        // Create Items
        var axeAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Strength, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-3000-0000-0000-000000000003") },
        };
        var axe = new EquipmentBase
        {
            Id = "00000000-3000-0000-0000-000000000003",
            IconPath = "iron_axe.png",
            Name = "Iron Axe",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = axeAttributes,
        };
        var daggerAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Dexterity, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-4000-0000-0000-000000000004") },
        };
        var dagger = new EquipmentBase
        {
            Id = "00000000-4000-0000-0000-000000000004",
            IconPath = "iron_dagger.png",
            Name = "Iron Dagger",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = daggerAttributes,
        };
        var hammerAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Endurance, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-5000-0000-0000-000000000005") },
        };
        var hammer = new EquipmentBase
        {
            Id = "00000000-5000-0000-0000-000000000005",
            IconPath = "iron_hammer.png",
            Name = "Iron Hammer",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = hammerAttributes,
        };
        var swordAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.FightingSpirit, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-1000-0000-0000-000000000001") } ,
        };
        var sword = new EquipmentBase
        {
            Id = "00000000-1000-0000-0000-000000000001",
            IconPath = "iron_sword.png",
            Name = "Iron Sword",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = swordAttributes,
        };
        var bowAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Agility, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-2000-0000-0000-000000000002") } ,
        };
        var bow = new EquipmentBase
        {
            Id = "00000000-2000-0000-0000-000000000002",
            IconPath = "bow.png",
            Name = "Bow",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = bowAttributes,
        };
        var shieldAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Constitution, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-6000-0000-0000-000000000006") } ,
        };
        var shield = new EquipmentBase
        {
            Id = "00000000-6000-0000-0000-000000000006",
            IconPath = "shield.png",
            Name = "Shield",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.OffHand,
            AttributeModifiers = shieldAttributes,
        };
        var staffAttributes = new List<ItemAttributeModifier>()
        {
            new(AttributeType.Intelligence, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = Guid.Parse("00000000-7000-0000-0000-000000000007") } ,
        };
        var staff = new EquipmentBase
        {
            Id = "00000000-7000-0000-0000-000000000007",
            IconPath = "staff.png",
            Name = "Staff",
            Description = "Worn down through years of use.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.MainHand,
            AttributeModifiers = staffAttributes,
        };

        var potion = new ItemBase
        {
            Id = "Guid.NewGuid()",
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

    public static async Task SeedWoodcuttingLootTables(LLDbContext context)
    {
        // Create Items for Tree Drops
        var treeLog = new ItemBase { Id = "Guid.NewGuid()151345",
            IconPath = "reward-item.png",
            Name = "Tree Log" };
        var nest = new ItemBase { Id = "Guid.NewGuid()312514",
            IconPath = "reward-item.png",
            Name = "Nest" };
        
        var oakLog = new ItemBase { Id = "Guid.NewGuid()2223",
            IconPath = "reward-item.png",
            Name = "Oak Log" };
        
        var birchLog = new ItemBase { Id = "Guid.NewGuid()321",
            IconPath = "reward-item.png",
            Name = "Birch Log" };
        var rareHerb = new ItemBase { Id = "Guid.NewGuid()123",
            IconPath = "reward-item.png",   
            Name = "Rare Herb" };

        // Add items to context
        await context.ItemBases.AddRangeAsync(treeLog, nest, oakLog, birchLog, rareHerb);

        // Create LootTableRarities for Tree
        var treeLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = nest.Id, Weight = 1 }],
            Weight = 1 // 0.01% chance to drop nest. 144%~ chance in 24 hours.
        };
        var treeLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = treeLog.Id, Weight = 20 }],
            Weight = 80 // 16%
        };
        var treeLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [treeLootTableCommon, treeLootTableLegendary]
        };

        // Create LootTableItems for Oak Tree
        var oakLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = nest.Id, Weight = 1 }],
            Weight = 2 // 0.02% chance to drop nest. 144%~ chance in 12 hours.
        };
        var oakLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = oakLog.Id, Weight = 20 }],
            Weight = 80 // 16%
        };
        var oakLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [oakLootTableCommon, oakLootTableLegendary]
        };

        // Create LootTableItems for Birch Tree
        var birchLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = nest.Id, Weight = 1 }],
            Weight = 3 // 0.04% chance to drop nest. 144%~ chance in 9 hours.
        };
        var birchLootTableRare = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = rareHerb.Id, Weight = 30 }],
            Weight = 15 // 4.5%
        };
        var birchLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { Id = Guid.NewGuid(), ItemId = birchLog.Id, Weight = 20 }],
            Weight = 80 // 16%
        };
        var birchLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [birchLootTableCommon, birchLootTableRare, birchLootTableLegendary]
        };

        // Add LootTables to context
        await context.LootTables.AddRangeAsync(treeLootTable, treeLootTableCommon, treeLootTableLegendary, oakLootTable, oakLootTableCommon, oakLootTableLegendary, birchLootTable, birchLootTableCommon, birchLootTableRare, birchLootTableLegendary);

        var treeGatheringNode = new GatheringNode { Id = "woodcutting_tree", Name = "Tree", GatheringType = GatheringType.Woodcutting, LootTableId = treeLootTable.Id };
        var oakGatheringNode = new GatheringNode { Id = "woodcutting_oak", Name = "Oak Tree", GatheringType = GatheringType.Woodcutting, LootTableId = oakLootTable.Id };
        var birchGatheringNode = new GatheringNode { Id = "woodcutting_birch", Name = "Birch Tree", GatheringType = GatheringType.Woodcutting, LootTableId = birchLootTable.Id };

        await context.GatheringNodes.AddRangeAsync(treeGatheringNode, oakGatheringNode, birchGatheringNode);
    }
}