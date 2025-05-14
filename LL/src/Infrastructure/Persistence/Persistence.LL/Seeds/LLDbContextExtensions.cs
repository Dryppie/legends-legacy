using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Colosseum;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.GatheringNodes;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.Masteries;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;
using Persistence.LL.Seeds.Seeding;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";

    public static async Task SeedData(this LLDbContext context, IPasswordHasher<AppUser> hasher)
    {
        if (!context.Entities.Any())
        {
            await DbJsonSeeder.RunAsync(context);
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
        await SeedMiningLootTables(context);
        await SeedWoodcuttingLootTables(context);
    }

    public static async Task SeedInventoryItems(LLDbContext context)
    {
        if (!context.InventoryItems.Any())
        {
            var goblinEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                ItemBaseId = "goblinId",
            };
            var ratEssenceItemInstance = new EssenceItemInstance
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                ItemBaseId = "largeRatId",
            };
            //var swordEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.SWORD_GUID),
            //    ItemBaseId = SeedItems.SWORD_GUID,
            //};
            //var bowEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.BOW_GUID),
            //    ItemBaseId = SeedItems.BOW_GUID,
            //};
            //var axeEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.AXE_GUID),
            //    ItemBaseId = SeedItems.AXE_GUID,
            //};
            //var daggerEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.DAGGER_GUID),
            //    ItemBaseId = SeedItems.DAGGER_GUID,
            //};
            //var hammerEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.HAMMER_GUID),
            //    ItemBaseId = SeedItems.HAMMER_GUID,
            //};
            //var shieldEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.SHIELD_GUID),
            //    ItemBaseId = SeedItems.SHIELD_GUID,
            //};
            //var staffEquipmentInstance = new EquipmentInstance
            //{
            //    Id = Guid.Parse(SeedItems.STAFF_GUID),
            //    ItemBaseId = SeedItems.STAFF_GUID,
            //};
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

            //var inventoryItemSword = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.SWORD_GUID), // Copied directly from SwordItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemBow = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.BOW_GUID), // Copied directly from BowItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemAxe = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.AXE_GUID), // Copied directly from AxeItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemDagger = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.DAGGER_GUID), // Copied directly from DaggerItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemHammer = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.HAMMER_GUID), // Copied directly from HammerItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemShield = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.SHIELD_GUID), // Copied directly from ShieldItem. Same ID
            //    Quantity = 1
            //};
            //var inventoryItemStaff = new InventoryItem()
            //{
            //    InventoryId = Guid.Parse(CHARACTER_GUID),
            //    ItemInstanceId = Guid.Parse(SeedItems.STAFF_GUID), // Copied directly from StaffItem. Same ID
            //    Quantity = 1
            //};
            await context.ItemInstances.AddRangeAsync(goblinEssenceItemInstance, ratEssenceItemInstance/*, swordEquipmentInstance, bowEquipmentInstance, axeEquipmentInstance, daggerEquipmentInstance, hammerEquipmentInstance, shieldEquipmentInstance, staffEquipmentInstance*/);
            await context.InventoryItems.AddRangeAsync(inventoryItemGoblinEssence, inventoryItemRatEssence/*, inventoryItemSword, inventoryItemBow, inventoryItemAxe, inventoryItemDagger, inventoryItemHammer, inventoryItemShield, inventoryItemStaff*/);
        }
    }

    public static async Task SeedOddGear(LLDbContext context)
    {
        // Create Items
        //var axeAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Strength, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-3000-0000-0000-000000000003" },
        //};
        //var axe = new EquipmentBase
        //{
        //    Id = "00000000-3000-0000-0000-000000000003",
        //    Name = "Iron Axe",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = axeAttributes,
        //};
        //var daggerAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Dexterity, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-4000-0000-0000-000000000004" },
        //};
        //var dagger = new EquipmentBase
        //{
        //    Id = "00000000-4000-0000-0000-000000000004",
        //    Name = "Iron Dagger",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = daggerAttributes,
        //};
        //var hammerAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Endurance, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-5000-0000-0000-000000000005" },
        //};
        //var hammer = new EquipmentBase
        //{
        //    Id = "00000000-5000-0000-0000-000000000005",
        //    Name = "Iron Hammer",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = hammerAttributes,
        //};
        //var swordAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.FightingSpirit, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-1000-0000-0000-000000000001" } ,
        //};
        //var sword = new EquipmentBase
        //{
        //    Id = "00000000-1000-0000-0000-000000000001",
        //    Name = "Iron Sword",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = swordAttributes,
        //};
        //var bowAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Agility, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-2000-0000-0000-000000000002" } ,
        //};
        //var bow = new EquipmentBase
        //{
        //    Id = "00000000-2000-0000-0000-000000000002",
        //    Name = "Bow",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = bowAttributes,
        //};
        //var shieldAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Constitution, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-6000-0000-0000-000000000006" } ,
        //};
        //var shield = new EquipmentBase
        //{
        //    Id = "00000000-6000-0000-0000-000000000006",
        //    Name = "Shield",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.OffHand,
        //    AttributeModifiers = shieldAttributes,
        //};
        //var staffAttributes = new List<ItemAttributeModifier>()
        //{
        //    new(AttributeType.Intelligence, 5, ModifierType.Flat) { Id = Guid.NewGuid(), ItemBaseId = "00000000-7000-0000-0000-000000000007" } ,
        //};
        //var staff = new EquipmentBase
        //{
        //    Id = "00000000-7000-0000-0000-000000000007",
        //    Name = "Staff",
        //    Description = "Worn down through years of use.",
        //    Rarity = Rarity.Common,
        //    EquipmentType = EquipmentType.MainHand,
        //    AttributeModifiers = staffAttributes,
        //};

        //var potion = new ItemBase
        //{
        //    Id = "Guid.NewGuid()",
        //    Name = "Potion"
        //};

        //var swordLTI = new LootTableItem
        //{
        //    ItemId = sword.Id,
        //};
        //var axeLTI = new LootTableItem
        //{
        //    ItemId = axe.Id,
        //};
        //var daggerLTI = new LootTableItem
        //{
        //    ItemId = dagger.Id,
        //};
        //var hammerLTI = new LootTableItem
        //{
        //    ItemId = hammer.Id,
        //};
        //var shieldLTI = new LootTableItem
        //{
        //    ItemId = shield.Id,
        //};
        //var staffLTI = new LootTableItem
        //{
        //    ItemId = staff.Id,
        //};

        //var bowLTI = new LootTableItem
        //{
        //    ItemId = bow.Id,
        //};

        //var potionLTI = new LootTableItem
        //{
        //    ItemId = potion.Id,
        //};


        //await context.ItemBases.AddRangeAsync(axe, dagger, hammer, sword, bow, shield, staff, potion);

        //await context.LootTableItems.AddRangeAsync(swordLTI, bowLTI, axeLTI, daggerLTI, hammerLTI, shieldLTI, staffLTI, potionLTI);

        //// Create LootTable and associate items with it
        //var lootTable = new LootTable
        //{
        //    Id = Guid.NewGuid(),
        //    Entries = [swordLTI, bowLTI, potionLTI]
        //};

        //await context.LootTables.AddAsync(lootTable);
    }

    public static async Task SeedMiningLootTables(LLDbContext context)
    {
        /* ────────────────────────────────
         *  Existing ItemBase IDs
         * ────────────────────────────────*/
        const string STONE_ID = "stone";
        const string FLINT_ID = "flint";
        const string TINY_GEODE_ID = "tiny_geode";
        const string JAGGED_OBSIDIAN_ID = "jagged_obsidian";
        const string CRYSTALLINE_POWDER_ID = "crystalline_powder";

        /* ────────────────────────────────
         *  Helper builders
         * ────────────────────────────────*/
        LootTable MakeItemTable(string itemId, int entryWeight, int tableWeight) =>
            new()
            {
                Id = Guid.NewGuid(),
                Weight = tableWeight,
                Entries =
                [
                    new LootTableItem
                {
                    Id     = Guid.NewGuid(),
                    ItemId = itemId,
                    Weight = entryWeight
                }
                ]
            };

        LootTable BuildLootTable(params LootTable[] subtables) =>
            new() { Id = Guid.NewGuid(), Entries = subtables };

        /* ────────────────────────────────
         *  Mining tiers & weights
         * ────────────────────────────────*/
        var miningCommon = MakeItemTable(STONE_ID, 20, 80); // 16 %
        var miningUncommon = MakeItemTable(FLINT_ID, 30, 30); // 9 %
        var miningRare = MakeItemTable(TINY_GEODE_ID, 1, 15); // 0.15 %
        var miningEpic = MakeItemTable(JAGGED_OBSIDIAN_ID, 30, 3); // 0.9 %
        var miningLegendary = MakeItemTable(CRYSTALLINE_POWDER_ID, 1, 1); // 0.03 %

        var miningRoot = BuildLootTable(
            miningCommon, miningUncommon, miningRare, miningEpic, miningLegendary);

        /* ────────────────────────────────
         *  Persist
         * ────────────────────────────────*/
        await context.LootTables.AddRangeAsync(
            miningRoot, miningCommon, miningUncommon,
            miningRare, miningEpic, miningLegendary);

        var miningNode = new GatheringNode
        {
            Id = "mining_slate_shard",
            Name = "Slate Shard",
            GatheringType = GatheringType.Mining,
            LootTableId = miningRoot.Id
        };

        await context.GatheringNodes.AddAsync(miningNode);
    }

    public static async Task SeedWoodcuttingLootTables(LLDbContext context)
    {
        const string WILLOW_LOG_ID = "willow_log";
        const string STICKY_SAP_ID = "sticky_sap";
        const string FEATHER_NEST_ID = "feather_lined_nest";
        const string SILK_VINE_ID = "silk_vine";
        const string SHIMMER_LEAF_ID = "shimmering_leaf";

        LootTable MakeItemTable(string itemId, int entryWeight, int tableWeight) =>
            new()
            {
                Id = Guid.NewGuid(),
                Weight = tableWeight,
                Entries =
                [
                    new LootTableItem
                    {
                        Id     = Guid.NewGuid(),
                        ItemId = itemId,
                        Weight = entryWeight
                    }
                ]
            };

        LootTable BuildLootTable(params LootTable[] subtables) =>
            new() { Id = Guid.NewGuid(), Entries = subtables };

        var willowCommon = MakeItemTable(WILLOW_LOG_ID, 20, 80); // 16 %
        var willowUncommon = MakeItemTable(STICKY_SAP_ID, 30, 30); // 9 %
        var willowRare = MakeItemTable(FEATHER_NEST_ID, 1, 15); // 0.15 %
        var willowEpic = MakeItemTable(SILK_VINE_ID, 30, 3);  // 0.9 %
        var willowLegendary = MakeItemTable(SHIMMER_LEAF_ID, 1, 1);  // 0.03 %
        var willowRoot = BuildLootTable(
            willowCommon, willowUncommon, willowRare, willowEpic, willowLegendary);

        // Add items to context
        await context.LootTables.AddRangeAsync(willowRoot, willowCommon, willowUncommon,
            willowRare, willowEpic, willowLegendary);
        
        var willowGatheringNode = new GatheringNode
        {
            Id = "woodcutting_young_willow",
            Name = "Young Willow",
            GatheringType = GatheringType.Woodcutting,
            LootTableId = willowRoot.Id
        };

        await context.GatheringNodes.AddRangeAsync(willowGatheringNode);
    }
}