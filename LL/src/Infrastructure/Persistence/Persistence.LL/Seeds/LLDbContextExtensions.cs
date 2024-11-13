using Domain.Helpers;
using Domain.Models.Abilities;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace Persistence.LL.Seeds;
public static class LLDbContextExtensions
{
    public static async Task SeedData(this LLDbContext context, UserManager<AppUser> userManager)
    {
        var email = "admin@hotmail.com";
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = "admin",
                Email = email
            };

            await userManager.CreateAsync(user, "Password123!");

            var character = new Character()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
            };

            var inventory = new Inventory()
            {
                CharacterId = character.Id,
            };

            context.Characters.Add(character);
            context.Inventories.Add(inventory);

            var attributes = EntityBaseAttributeHelper.CreateEntityAttributes(character.Id);
            await context.EntityAttributes.AddRangeAsync(attributes);

            var abilities = new List<AbilityId>()
            {
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "fireball_01"
                },
                new AbilityId()
                {
                    EntityId = character.Id,
                    Id = "heal_01"
                }
            };

            await context.AbilityIds.AddRangeAsync(abilities);
        }

        await SeedCreaturesAndLootTablesForShenicRegionLumoRuins(context);

        await SeedItemsAndLootTables(context);
        
        await context.SaveChangesAsync();
    }


    private static async Task SeedCreaturesAndLootTablesForShenicRegionLumoRuins(LLDbContext context)
    {
        if (!context.Creatures.Any())
        {
            // Define creature IDs
            var goblinId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var goblinWarriorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var goblinArcherId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var largeRatId = Guid.Parse("00000000-0000-0000-0000-000000000004");

            // Create creatures
            var lumoRuinsCreatures = new List<Creature>
            {
                new Creature { Id = goblinId, Name = "Goblin" },
                new Creature { Id = goblinWarriorId, Name = "Goblin Warrior" },
                new Creature { Id = goblinArcherId, Name = "Goblin Archer" },
                new Creature { Id = largeRatId, Name = "Large Rat" }
            };

            await context.Creatures.AddRangeAsync(lumoRuinsCreatures);

            // Create attributes
            var attributes = new List<EntityAttribute>();
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinWarriorId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinArcherId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(largeRatId));

            await context.EntityAttributes.AddRangeAsync(attributes);
            var abilityIds = new List<AbilityId>()
            {
                new AbilityId()
                {
                    EntityId = goblinId,
                    Id = "sneakAttack"
                },
                new AbilityId()
                {
                    EntityId = goblinId,
                    Id = "pocketDirt"
                },
                new AbilityId()
                {
                    EntityId = goblinWarriorId,
                    Id = "recklessAssault"
                },
                new AbilityId()
                {
                    EntityId = goblinWarriorId,
                    Id = "ragingCleave"
                },
                new AbilityId()
                {
                    EntityId = goblinArcherId,
                    Id = "poisonedArrows"
                },
                new AbilityId()
                {
                    EntityId = goblinArcherId,
                    Id = "snipersStrike"
                },
                new AbilityId()
                {
                    EntityId = largeRatId,
                    Id = "big"
                },
                new AbilityId()
                {
                    EntityId = largeRatId,
                    Id = "tailWrap"
                }
            };
            await context.AbilityIds.AddRangeAsync(abilityIds);

            var goblinEssence = new Essence
            {
                Id = Guid.NewGuid(),
                Name = "Goblin's Essence",
                PassiveAbilityId = "pocketDirt",
                ActiveAbilityId = "sneakAttack"
            };

            var goblinWarriorEssence = new Essence
            {
                Id = Guid.NewGuid(),
                Name = "Goblin Warrior's Essence",
                PassiveAbilityId = "recklessAssault",
                ActiveAbilityId = "ragingCleave"
            };

            var goblinArcherEssence = new Essence
            {
                Id = Guid.NewGuid(),
                Name = "Goblin Archer's Essence",
                PassiveAbilityId = "poisonedArrows",
                ActiveAbilityId = "snipersStrike"
            };

            var largeRatEssence = new Essence
            {
                Id = Guid.NewGuid(),
                Name = "Large Rat's Essence",
                PassiveAbilityId = "big",
                ActiveAbilityId = "tailWrap"
            };
            // Add items to context
            await context.Items.AddRangeAsync(goblinEssence, goblinWarriorEssence, goblinArcherEssence, largeRatEssence);

            var goblinEssenceLootTableItem = new LootTableItem { ItemId = goblinEssence.Id, Weight = 2 };
            var goblinWarriorEssenceLootTableItem = new LootTableItem { ItemId = goblinWarriorEssence.Id, Weight = 1 };
            var goblinArcherEssenceLootTableItem = new LootTableItem { ItemId = goblinArcherEssence.Id, Weight = 1 };
            var largeRatEssenceLootTableItem = new LootTableItem { ItemId = largeRatEssence.Id, Weight = 2 };

            // Create LootTableRarities for Goblin
            var goblinLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinEssenceLootTableItem],
                Weight = 1 // 0.02%
            };
            var goblinLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinLootTableLegendary]
            };
            // Create LootTableRarities for Goblin Warrior
            var goblinWarriorLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinWarriorEssenceLootTableItem],
                Weight = 1 // 0.01%
            };
            var goblinWarriorLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinWarriorLootTableLegendary]
            };
            // Create LootTableRarities for Goblin Archer
            var goblinArcherLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinArcherEssenceLootTableItem],
                Weight = 1 // 0.01%
            };
            var goblinArcherLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinArcherLootTableLegendary]
            };
            // Create LootTableRarities for Large Rat
            var largeRatLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [largeRatEssenceLootTableItem],
                Weight = 1 // 0.02%
            };
            var largeRatLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [largeRatLootTableLegendary]
            };
            await context.LootTables.AddRangeAsync(goblinLootTable, goblinWarriorLootTable, goblinArcherLootTable, largeRatLootTable);

            if (!context.Regions.Any())
            {
                var areas = new List<Area>()
                {
                    new Area
                    {
                        Name = "Lumo Ruins",
                        Creatures = lumoRuinsCreatures
                    }
                };

                await context.Areas.AddRangeAsync(areas);

                var regions = new List<Region>
                {
                    new Region()
                    {
                        Name = "Shenic",
                        Areas = areas
                    }
                };
                await context.Regions.AddRangeAsync(regions);
            }
        }
    }

    public static async Task SeedItemsAndLootTables(LLDbContext context)
    {
        if (!context.LootTables.Any() && !context.Items.Any())
        {
            await SeedOddGear(context);

            await SeedWoodcuttingLootTables(context);
        }
    }

    public static async Task SeedOddGear(LLDbContext context)
    {
        // Create Items
        var sword = new Item
        {
            Id = Guid.NewGuid(),
            Name = "Sword"
        };

        var shield = new Item
        {
            Id = Guid.NewGuid(),
            Name = "Shield"
        };

        var potion = new Item
        {
            Id = Guid.NewGuid(),
            Name = "Potion"
        };

        var swordLTI = new LootTableItem
        {
            ItemId = sword.Id,
        };

        var shieldLTI = new LootTableItem
        {
            ItemId = shield.Id,
        };

        var potionLTI = new LootTableItem
        {
            ItemId = potion.Id,
        };


        await context.Items.AddRangeAsync(sword, shield, potion);

        await context.LootTableItems.AddRangeAsync(swordLTI, shieldLTI, potionLTI);

        // Create LootTable and associate items with it
        var lootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = new List<LootTableEntry> { swordLTI, shieldLTI, potionLTI }
        };

        context.LootTables.Add(lootTable);
    }

    public static async Task SeedWoodcuttingLootTables(LLDbContext context)
    {
        // Create Items for Tree Drops
        var treeLog = new Item { Id = Guid.NewGuid(), Name = "Tree Log" };
        var nest = new Item { Id = Guid.NewGuid(), Name = "Nest" };
        
        var oakLog = new Item { Id = Guid.NewGuid(), Name = "Oak Log" };
        
        var birchLog = new Item { Id = Guid.NewGuid(), Name = "Birch Log" };
        var rareHerb = new Item { Id = Guid.NewGuid(), Name = "Rare Herb" };

        // Add items to context
        await context.Items.AddRangeAsync(treeLog, nest, oakLog, birchLog, rareHerb);

        var nestLootTableItem = new LootTableItem { ItemId = nest.Id, Weight = 1 };

        // Create LootTableRarities for Tree
        var treeLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [nestLootTableItem],
            Weight = 1 // 0.01% chance to drop nest. 144%~ chance in 24 hours.
        };
        var treeLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { ItemId = treeLog.Id, Weight = 20 }],
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
            Entries = [nestLootTableItem],
            Weight = 2 // 0.02% chance to drop nest. 144%~ chance in 12 hours.
        };
        var oakLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { ItemId = oakLog.Id, Weight = 20 }],
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
            Entries = [nestLootTableItem],
            Weight = 3 // 0.04% chance to drop nest. 144%~ chance in 9 hours.
        };
        var birchLootTableRare = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { ItemId = rareHerb.Id, Weight = 30 }],
            Weight = 15 // 4.5%
        };
        var birchLootTableCommon = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [new LootTableItem { ItemId = birchLog.Id, Weight = 20 }],
            Weight = 80 // 16%
        };
        var birchLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [birchLootTableCommon, birchLootTableRare, birchLootTableLegendary]
        };

        // Add LootTables to context
        await context.LootTables.AddRangeAsync(treeLootTable, oakLootTable, birchLootTable);
    }
}