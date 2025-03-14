using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.GatheringNodes;
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
    public const string CHARACTER_GUID = "11111111-1111-1111-1111-111111111111";

    public static async Task SeedData(this LLDbContext context, UserManager<AppUser> userManager)
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
            };

            character.EssenceSlots = essenceSlots;
            context.Characters.Add(character);
            context.Inventories.Add(inventory);
            await context.Essences.AddRangeAsync(essences);
        }

        await SeedCreaturesAndLootTablesForShenicRegionLumoRuins(context);

        await SeedItemsAndLootTables(context);

        await SeedInventoryItems(context);

        await context.SaveChangesAsync();
    }


    private static async Task SeedCreaturesAndLootTablesForShenicRegionLumoRuins(LLDbContext context)
    {
        if (!context.Creatures.Any())
        {
            // Step 1 - Creature Ids
            var goblinId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var goblinWarriorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var goblinArcherId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var largeRatId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var flameImpId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var frostImpId = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var shadowImpId = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var vampireBatId = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var blueSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var brownSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var greenSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var rainbowSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var redSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var transparentSlimeId = Guid.Parse("00000000-0000-0000-0000-000000000014");

            // Step 2 - Essences
            var goblinEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Goblin's Essence",
                ActiveAbilityId = "sneakAttack",
                PassiveAbilityId = "pocketDirt"
            };
            var goblinWarriorEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Goblin Warrior's Essence",
                ActiveAbilityId = "ragingCleave",
                PassiveAbilityId = "recklessAssault"
            };
            var goblinArcherEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Goblin Archer's Essence",
                ActiveAbilityId = "snipersStrike",
                PassiveAbilityId = "poisonedArrows"
            };
            var largeRatEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Large Rat's Essence",
                ActiveAbilityId = "tailWrap",
                PassiveAbilityId = "big",
            };
            var flameImpEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Flame Imp's Essence",
                ActiveAbilityId = "firebombToss",
                PassiveAbilityId = "hotAura",
            };
            var frostImpEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Frost Imp's Essence",
                ActiveAbilityId = "iceTouch",
                PassiveAbilityId = "coldAura",
            };
            var shadowImpEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Shadow Imp's Essence",
                ActiveAbilityId = "shadowImage",
                PassiveAbilityId = "shadowyPresence",
            };
            var vampireBatEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Vampire Bat's Essence",
                ActiveAbilityId = "bloodthirstyFangs",
                PassiveAbilityId = "darkVitality",
            };
            var blueSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Blue Slime's Essence",
                ActiveAbilityId = "sweetWater",
                PassiveAbilityId = "absorptiveShell",
            };
            var brownSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Brown Slime's Essence",
                ActiveAbilityId = "mudArmor",
                PassiveAbilityId = "earthlyFortitude",
            };
            var greenSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Green Slime's Essence",
                ActiveAbilityId = "acidSplash",
                PassiveAbilityId = "corrosiveOoze",
            };
            var rainbowSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Rainbow Slime's Essence",
                ActiveAbilityId = "unstableColors",
                PassiveAbilityId = "colorfulShield",
            };
            var redSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Red Slime's Essence",
                ActiveAbilityId = "igniteCore",
                PassiveAbilityId = "fireBody",
            };
            var transparentSlimeEssence = new Essence()
            {
                Id = Guid.NewGuid(),
                Name = "Transparent Slime's Essence",
                ActiveAbilityId = "transparentEngulf",
                PassiveAbilityId = "transparentShift",
            };

            // Step 3 - Essence Items
            var goblinEssenceItem = new EssenceItem
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                IconPath = "essence-item.svg",
                Name = goblinEssence.Name,
                Essence = goblinEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var goblinWarriorEssenceItem = new EssenceItem
            {
                Id = Guid.NewGuid(),
                IconPath = "essence-item.svg",
                Name = goblinWarriorEssence.Name,
                Essence = goblinWarriorEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var goblinArcherEssenceItem = new EssenceItem
            {
                Id = Guid.NewGuid(),
                IconPath = "essence-item.svg",
                Name = goblinArcherEssence.Name,
                Essence = goblinArcherEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var largeRatEssenceItem = new EssenceItem
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                IconPath = "essence-item.svg",
                Name = largeRatEssence.Name,
                Essence = largeRatEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var flameImpEssenceItem = new EssenceItem
            {
                Id = flameImpId,
                IconPath = "essence-item.svg",
                Name = flameImpEssence.Name,
                Essence = flameImpEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var frostImpEssenceItem = new EssenceItem
            {
                Id = frostImpId,
                IconPath = "essence-item.svg",
                Name = frostImpEssence.Name,
                Essence = frostImpEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var shadowImpEssenceItem = new EssenceItem
            {
                Id = shadowImpId,
                IconPath = "essence-item.svg",
                Name = shadowImpEssence.Name,
                Essence = shadowImpEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var vampireBatEssenceItem = new EssenceItem
            {
                Id = vampireBatId,
                IconPath = "essence-item.svg",
                Name = vampireBatEssence.Name,
                Essence = vampireBatEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var blueSlimeEssenceItem = new EssenceItem
            {
                Id = blueSlimeId,
                IconPath = "essence-item.svg",
                Name = blueSlimeEssence.Name,
                Essence = blueSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var brownSlimeEssenceItem = new EssenceItem
            {
                Id = brownSlimeId,
                IconPath = "essence-item.svg",
                Name = brownSlimeEssence.Name,
                Essence = brownSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var greenSlimeEssenceItem = new EssenceItem
            {
                Id = greenSlimeId,
                IconPath = "essence-item.svg",
                Name = greenSlimeEssence.Name,
                Essence = greenSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var rainbowSlimeEssenceItem = new EssenceItem
            {
                Id = rainbowSlimeId,
                IconPath = "essence-item.svg",
                Name = rainbowSlimeEssence.Name,
                Essence = rainbowSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var redSlimeEssenceItem = new EssenceItem
            {
                Id = redSlimeId,
                IconPath = "essence-item.svg",
                Name = redSlimeEssence.Name,
                Essence = redSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };
            var transparentSlimeEssenceItem = new EssenceItem
            {
                Id = transparentSlimeId,
                IconPath = "essence-item.svg",
                Name = transparentSlimeEssence.Name,
                Essence = transparentSlimeEssence,
                ItemType = ItemType.Essence,
                Rarity = Rarity.Unique
            };

            // Step 4 - Loot Tables
            var goblinEssenceLootTableItem = new LootTableItem { ItemId = goblinEssenceItem.Id, Weight = 50 };
            var goblinWarriorEssenceLootTableItem = new LootTableItem { ItemId = goblinWarriorEssenceItem.Id, Weight = 50 };
            var goblinArcherEssenceLootTableItem = new LootTableItem { ItemId = goblinArcherEssenceItem.Id, Weight = 50 };
            var largeRatEssenceLootTableItem = new LootTableItem { ItemId = largeRatEssenceItem.Id, Weight = 50 };
            var flameImpEssenceLootTableItem = new LootTableItem { ItemId = flameImpEssenceItem.Id, Weight = 50 };
            var frostImpEssenceLootTableItem = new LootTableItem { ItemId = frostImpEssenceItem.Id, Weight = 50 };
            var shadowImpEssenceLootTableItem = new LootTableItem { ItemId = shadowImpEssenceItem.Id, Weight = 50 };
            var vampireBatEssenceLootTableItem = new LootTableItem { ItemId = vampireBatEssenceItem.Id, Weight = 50 };
            var blueSlimeEssenceLootTableItem = new LootTableItem { ItemId = blueSlimeEssenceItem.Id, Weight = 50 };
            var brownSlimeEssenceLootTableItem = new LootTableItem { ItemId = brownSlimeEssenceItem.Id, Weight = 50 };
            var greenSlimeEssenceLootTableItem = new LootTableItem { ItemId = greenSlimeEssenceItem.Id, Weight = 50 };
            var rainbowSlimeEssenceLootTableItem = new LootTableItem { ItemId = rainbowSlimeEssenceItem.Id, Weight = 50 };
            var redSlimeEssenceLootTableItem = new LootTableItem { ItemId = redSlimeEssenceItem.Id, Weight = 50 };
            var transparentSlimeEssenceLootTableItem = new LootTableItem { ItemId = transparentSlimeEssenceItem.Id, Weight = 50 };

            // Create LootTableRarities for Goblin
            var goblinLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [goblinEssenceLootTableItem],
                Weight = 30 // 0.02%
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
                Weight = 30 // 0.01%
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
                Weight = 30 // 0.01%
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
                Weight = 30 // 0.02%
            };
            var largeRatLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [largeRatLootTableLegendary]
            };
            // Create LootTableRarities for Flame Imp
            var flameImpLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [flameImpEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var flameImpLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [flameImpLootTableLegendary]
            };
            // Create LootTableRarities for Frost Imp
            var frostImpLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [frostImpEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var frostImpLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [frostImpLootTableLegendary]
            };
            // Create LootTableRarities for Shadow Imp
            var shadowImpLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [shadowImpEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var shadowImpLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [shadowImpLootTableLegendary]
            };
            // Create LootTableRarities for Vampire Bat
            var vampireBatLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [vampireBatEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var vampireBatLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [vampireBatLootTableLegendary]
            };
            // Create LootTableRarities for Blue Slime
            var blueSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [blueSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var blueSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [blueSlimeLootTableLegendary]
            };
            // Create LootTableRarities for Brown Slime
            var brownSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [brownSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var brownSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [brownSlimeLootTableLegendary]
            };
            // Create LootTableRarities for Green Slime
            var greenSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [greenSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var greenSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [greenSlimeLootTableLegendary]
            };
            // Create LootTableRarities for Rainbow Slime
            var rainbowSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [rainbowSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var rainbowSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [rainbowSlimeLootTableLegendary]
            };
            // Create LootTableRarities for Red Slime
            var redSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [redSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var redSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [redSlimeLootTableLegendary]
            };
            // Create LootTableRarities for Transparent Slime
            var transparentSlimeLootTableLegendary = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [transparentSlimeEssenceLootTableItem],
                Weight = 30 // 0.02%
            };
            var transparentSlimeLootTable = new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = [transparentSlimeLootTableLegendary]
            };


            await context.Items.AddRangeAsync(goblinEssenceItem, goblinWarriorEssenceItem, goblinArcherEssenceItem, largeRatEssenceItem);
            await context.Items.AddRangeAsync(flameImpEssenceItem, frostImpEssenceItem, shadowImpEssenceItem, vampireBatEssenceItem);
            await context.Items.AddRangeAsync(blueSlimeEssenceItem, brownSlimeEssenceItem, greenSlimeEssenceItem, rainbowSlimeEssenceItem, redSlimeEssenceItem, transparentSlimeEssenceItem);
            await context.Essences.AddRangeAsync(goblinEssence, goblinWarriorEssence, goblinArcherEssence, largeRatEssence);
            await context.Essences.AddRangeAsync(flameImpEssence, frostImpEssence, shadowImpEssence, vampireBatEssence);
            await context.Essences.AddRangeAsync(blueSlimeEssence, brownSlimeEssence, greenSlimeEssence, rainbowSlimeEssence, redSlimeEssence, transparentSlimeEssence);
            await context.LootTables.AddRangeAsync(goblinLootTable, goblinWarriorLootTable, goblinArcherLootTable, largeRatLootTable);
            await context.LootTables.AddRangeAsync(flameImpLootTable, frostImpLootTable, shadowImpLootTable, vampireBatLootTable);
            await context.LootTables.AddRangeAsync(blueSlimeLootTable, brownSlimeLootTable, greenSlimeLootTable, rainbowSlimeLootTable, redSlimeLootTable, transparentSlimeLootTable);

            var goblinEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = goblinEssence,
                EntityId = goblinId
            };
            var goblinWarriorEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = goblinWarriorEssence,
                EntityId = goblinWarriorId
            };
            var goblinArcherEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = goblinArcherEssence,
                EntityId = goblinArcherId
            };
            var largeRatEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = largeRatEssence,
                EntityId = largeRatId
            };

            var flameImpEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = flameImpEssence,
                EntityId = flameImpId
            };
            var frostImpEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = frostImpEssence,
                EntityId = frostImpId
            };
            var shadowImpEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = shadowImpEssence,
                EntityId = shadowImpId
            };
            var vampireBatEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = vampireBatEssence,
                EntityId = vampireBatId
            };

            var blueSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = blueSlimeEssence,
                EntityId = blueSlimeId
            };
            var brownSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = brownSlimeEssence,
                EntityId = brownSlimeId
            };
            var greenSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = greenSlimeEssence,
                EntityId = greenSlimeId
            };
            var rainbowSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = rainbowSlimeEssence,
                EntityId = rainbowSlimeId
            };
            var redSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = redSlimeEssence,
                EntityId = redSlimeId
            };
            var transparentSlimeEssenceSlot = new EssenceSlot()
            {
                Id = Guid.NewGuid(),
                SlotState = SlotState.Active,
                SlotType = SlotType.Standard,
                OccupiedEssence = transparentSlimeEssence,
                EntityId = transparentSlimeId
            };

            // Step 5 - Create creatures
            var lumoRuinsCreatures = new List<Creature>
            {
                new() { Id = goblinId, Name = "Goblin", LootTableId = goblinLootTable.Id, EssenceSlots = [goblinEssenceSlot], ExperienceReward = 2 },
                new() { Id = goblinWarriorId, Name = "Goblin Warrior", LootTableId = goblinWarriorLootTable.Id, EssenceSlots = [goblinWarriorEssenceSlot], ExperienceReward = 3 },
                new() { Id = goblinArcherId, Name = "Goblin Archer", LootTableId = goblinArcherLootTable.Id, EssenceSlots = [goblinArcherEssenceSlot], ExperienceReward = 3 },
                new() { Id = largeRatId, Name = "Large Rat", LootTableId = largeRatLootTable.Id, EssenceSlots = [largeRatEssenceSlot], ExperienceReward = 2 }
            };

            var bloodGroveCreatures = new List<Creature>
            {
                new() { Id = flameImpId, Name = "Flame Imp", LootTableId = flameImpLootTable.Id, EssenceSlots = [flameImpEssenceSlot], ExperienceReward = 2 },
                new() { Id = frostImpId, Name = "Frost Imp", LootTableId = frostImpLootTable.Id, EssenceSlots = [frostImpEssenceSlot], ExperienceReward = 2 },
                new() { Id = shadowImpId, Name = "Shadow Imp", LootTableId = shadowImpLootTable.Id, EssenceSlots = [shadowImpEssenceSlot], ExperienceReward = 2 },
                new() { Id = vampireBatId, Name = "Vampire Bat", LootTableId = vampireBatLootTable.Id, EssenceSlots = [vampireBatEssenceSlot], ExperienceReward = 4 }
            };

            var crystalCreekCreatures = new List<Creature>
            {
                new() { Id = blueSlimeId, Name = "Blue Slime", LootTableId = blueSlimeLootTable.Id, EssenceSlots = [blueSlimeEssenceSlot], ExperienceReward = 3 },
                new() { Id = brownSlimeId, Name = "Brown Slime", LootTableId = brownSlimeLootTable.Id, EssenceSlots = [brownSlimeEssenceSlot], ExperienceReward = 4 },
                new() { Id = greenSlimeId, Name = "Green Slime", LootTableId = greenSlimeLootTable.Id, EssenceSlots = [greenSlimeEssenceSlot], ExperienceReward = 3 },
                new() { Id = rainbowSlimeId, Name = "Rainbow Slime", LootTableId = rainbowSlimeLootTable.Id, EssenceSlots = [rainbowSlimeEssenceSlot], ExperienceReward = 4 },
                new() { Id = redSlimeId, Name = "Red Slime", LootTableId = redSlimeLootTable.Id, EssenceSlots = [redSlimeEssenceSlot], ExperienceReward = 3 },
                new() { Id = transparentSlimeId, Name = "Transparent Slime", LootTableId = transparentSlimeLootTable.Id, EssenceSlots = [transparentSlimeEssenceSlot], ExperienceReward = 4 },
            };

            await context.Creatures.AddRangeAsync(lumoRuinsCreatures);
            await context.Creatures.AddRangeAsync(bloodGroveCreatures);
            await context.Creatures.AddRangeAsync(crystalCreekCreatures);

            // Step 6 - Create area
            var lumoRuinsAreaId = "region_01_area_01";
            var lumoRuinsAreaCreatures = new List<AreaCreature>
            {
                new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinId, WeightedSpawnRate = 0.45f },
                new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinWarriorId, WeightedSpawnRate = 0.2f },
                new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinArcherId, WeightedSpawnRate = 0.2f },
                new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = largeRatId, WeightedSpawnRate = 0.25f },
            };

            var bloodGroveAreaId = "region_01_area_02";
            var bloodGroveAreaCreatures = new List<AreaCreature>
            {
                new AreaCreature() { AreaId = bloodGroveAreaId, CreatureId = flameImpId, WeightedSpawnRate = 0.31f },
                new AreaCreature() { AreaId = bloodGroveAreaId, CreatureId = frostImpId, WeightedSpawnRate = 0.3f },
                new AreaCreature() { AreaId = bloodGroveAreaId, CreatureId = shadowImpId, WeightedSpawnRate = 0.3f },
                new AreaCreature() { AreaId = bloodGroveAreaId, CreatureId = vampireBatId, WeightedSpawnRate = 0.09f },
            };

            var crystalCreekAreaId = "region_01_area_03";
            var crystalCreekAreaCreatures = new List<AreaCreature>
            {
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = blueSlimeId, WeightedSpawnRate = 0.20f },
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = brownSlimeId, WeightedSpawnRate = 0.20f },
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = greenSlimeId, WeightedSpawnRate = 0.20f },
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = rainbowSlimeId, WeightedSpawnRate = 0.10f },
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = redSlimeId, WeightedSpawnRate = 0.20f },
                new AreaCreature() { AreaId = crystalCreekAreaId, CreatureId = transparentSlimeId, WeightedSpawnRate = 0.10f },
            };

            // Create attributes
            var attributes = new List<EntityAttribute>();
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinWarriorId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(goblinArcherId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(largeRatId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(flameImpId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(frostImpId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(shadowImpId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(vampireBatId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(blueSlimeId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(brownSlimeId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(greenSlimeId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(rainbowSlimeId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(redSlimeId));
            attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributes(transparentSlimeId));
            await context.EntityAttributes.AddRangeAsync(attributes);

            if (!context.Regions.Any())
            {
                var areas = new List<Area>()
                {
                    new Area
                    {
                        Id = lumoRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                        Name = "Lumo Ruins",
                        Creatures = lumoRuinsAreaCreatures,
                        SpawnProbabilities = new List<float>
                        {
                            0.87f,
                            0.09f,
                            0.03f,
                            0.01f,
                        }
                    },
                    new Area
                    {
                        Id = bloodGroveAreaId, // region, [area, dungeon, raid, or rift], area
                        Name = "Blood Grove",
                        Creatures = bloodGroveAreaCreatures,
                        SpawnProbabilities = new List<float>
                        {
                            0.82f,
                            0.12f,
                            0.04f,
                            0.02f,
                        }
                    },
                    new Area
                    {
                        Id = crystalCreekAreaId, // region, [area, dungeon, raid, or rift], area
                        Name = "Crystal Creek",
                        Creatures = crystalCreekAreaCreatures,
                        SpawnProbabilities = new List<float>
                        {
                            0.75f,
                            0.17f,
                            0.05f,
                            0.02f,
                            0.01f,
                        }
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

    public static async Task SeedInventoryItems(LLDbContext context)
    {
        if (!context.InventoryItems.Any())
        {
            var inventoryItemGoblinEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Copied directly from GoblinEssenceItem. Same ID
                Quantity = 1
            };

            var inventoryItemRatEssence = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemId = Guid.Parse("00000000-0000-0000-0000-000000000004"), // Copied directly from LargeRatEssenceItem. Same ID
                Quantity = 1
            };

            var inventoryItemSword = new InventoryItem()
            {
                InventoryId = Guid.Parse(CHARACTER_GUID),
                ItemId = Guid.Parse("00000000-0000-0000-0000-000000000005"), // Copied directly from SwordItem. Same ID
                Quantity = 1
            };

            await context.InventoryItems.AddRangeAsync(inventoryItemGoblinEssence, inventoryItemRatEssence, inventoryItemSword);
        }
    }

    public static async Task SeedOddGear(LLDbContext context)
    {
        // Create Items
        var sword = new Item
        {
            Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Sword"
        };

        var shield = new Item
        {
            Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Shield"
        };

        var potion = new Item
        {
            Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
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
        var treeLog = new Item { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Tree Log" };
        var nest = new Item { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Nest" };
        
        var oakLog = new Item { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Oak Log" };
        
        var birchLog = new Item { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Birch Log" };
        var rareHerb = new Item { Id = Guid.NewGuid(),
            IconPath = "reward-item.png",
            Name = "Rare Herb" };

        // Add items to context
        await context.Items.AddRangeAsync(treeLog, nest, oakLog, birchLog, rareHerb);

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