using Domain.Helpers;
using Domain.Models.Attributes;
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

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";

    public static async Task SeedData(this LLDbContext context, UserManager<AppUser> userManager)
    {
        if (!context.Entities.Any())
        {
            await SeedCreatures.SeedCreaturesData(context);
            await SeedItems.SeedItemsData(context);
            await SeedItemsAndLootTables(context);
            await SeedAdminData(context, userManager);

            await SeedInventoryItems(context);

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAdminData(LLDbContext context, UserManager<AppUser> userManager)
    {
        var email = "admin@hotmail.com";
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = "admin",
                Email = email,
                NormalizedUserName = "admin",
            };

            await userManager.CreateAsync(user, "Password123!");

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
            var equipmentSlots = SeedEquipmentSlots(character);
            var masteries = SeedMasteries(character);
            context.EquipmentSlots.AddRange(equipmentSlots);
            character.EssenceSlots = essenceSlots;
            character.Masteries = masteries;
            context.Characters.Add(character);
            context.Inventories.Add(inventory);
            await context.Essences.AddRangeAsync(essences);
        }
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
                ItemBaseId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            };
            var ratEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                ItemBaseId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            };
            var swordEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.SWORD_GUID),
                ItemBaseId = Guid.Parse(SeedItems.SWORD_GUID),
            };
            var bowEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.BOW_GUID),
                ItemBaseId = Guid.Parse(SeedItems.BOW_GUID),
            };
            var axeEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.AXE_GUID),
                ItemBaseId = Guid.Parse(SeedItems.AXE_GUID),
            };
            var daggerEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.DAGGER_GUID),
                ItemBaseId = Guid.Parse(SeedItems.DAGGER_GUID),
            };
            var hammerEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.HAMMER_GUID),
                ItemBaseId = Guid.Parse(SeedItems.HAMMER_GUID),
            };
            var shieldEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.SHIELD_GUID),
                ItemBaseId = Guid.Parse(SeedItems.SHIELD_GUID),
            };
            var staffEquipmentInstance = new EquipmentInstance
            {
                Id = Guid.Parse(SeedItems.STAFF_GUID),
                ItemBaseId = Guid.Parse(SeedItems.STAFF_GUID),
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

    public static async Task SeedWoodcuttingLootTables(LLDbContext context)
    {
        // Create Items for Tree Drops
        var treeLog = new ItemBase { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Tree Log" };
        var nest = new ItemBase { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Nest" };
        
        var oakLog = new ItemBase { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Oak Log" };
        
        var birchLog = new ItemBase { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Birch Log" };
        var rareHerb = new ItemBase { Id = Guid.NewGuid(),
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