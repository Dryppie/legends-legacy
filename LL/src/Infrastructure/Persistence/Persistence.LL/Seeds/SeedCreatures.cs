using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;

namespace Persistence.LL.Seeds;
public static class SeedCreatures
{
    public static async Task SeedCreaturesData(this LLDbContext context)
    {
        await SeedCreaturesAndLootTablesForShenicRegionLumoRuins(context);
    }

    private static async Task SeedCreaturesAndLootTablesForShenicRegionLumoRuins(LLDbContext context)
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
        
        var enchantedFairyId = Guid.Parse("00000000-0000-0000-0000-000000000015");
        var gladePantherId = Guid.Parse("00000000-0000-0000-0000-000000000016");
        var illusionFoxId = Guid.Parse("00000000-0000-0000-0000-000000000017");
        var nightshadeBlossomId = Guid.Parse("00000000-0000-0000-0000-000000000018");
        var pixieId = Guid.Parse("00000000-0000-0000-0000-000000000019");
        
        var hobgoblinId = Guid.Parse("00000000-0000-0000-0000-000000000020");

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
        var enchantedFairyEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Enchanted Fairy's Essence",
            ActiveAbilityId = "faesEmbrace",
            PassiveAbilityId = "enchantedCharm",
        };
        var gladePantherEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Glade Panther's Essence",
            ActiveAbilityId = "ambushStrike",
            PassiveAbilityId = "razorClaws",
        };
        var illusionFoxEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Illusion Fox's Essence",
            ActiveAbilityId = "distractingIllusion",
            PassiveAbilityId = "foxfireWisp",
        };
        var nightshadeBlossomEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Nightshade Blossom's Essence",
            ActiveAbilityId = "necroticSpores",
            PassiveAbilityId = "twilightBloom",
        };
        var pixieEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Pixie's Essence",
            ActiveAbilityId = "pixieBurst",
            PassiveAbilityId = "morningDew",
        };
        var hobgoblinEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Hobgoblin's Essence",
            ActiveAbilityId = "frenzy",
            PassiveAbilityId = "savageOnslaught",
        };

        // Step 3 - Essence Items
        var goblinEssenceItem = new EssenceItemBase
        {
            Id = "goblinId",
            IconPath = "essence-item.svg",
            Name = goblinEssence.Name,
            Essence = goblinEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var goblinWarriorEssenceItem = new EssenceItemBase
        {
            Id = "goblinWarriorId",
            IconPath = "essence-item.svg",
            Name = goblinWarriorEssence.Name,
            Essence = goblinWarriorEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var goblinArcherEssenceItem = new EssenceItemBase
        {
            Id = "goblinArcherId",
            IconPath = "essence-item.svg",
            Name = goblinArcherEssence.Name,
            Essence = goblinArcherEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var largeRatEssenceItem = new EssenceItemBase
        {
            Id = "largeRatId",
            IconPath = "essence-item.svg",
            Name = largeRatEssence.Name,
            Essence = largeRatEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var flameImpEssenceItem = new EssenceItemBase
        {
            Id = "flameImpId",
            IconPath = "essence-item.svg",
            Name = flameImpEssence.Name,
            Essence = flameImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var frostImpEssenceItem = new EssenceItemBase
        {
            Id = "frostImpId",
            IconPath = "essence-item.svg",
            Name = frostImpEssence.Name,
            Essence = frostImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var shadowImpEssenceItem = new EssenceItemBase
        {
            Id = "shadowImpId",
            IconPath = "essence-item.svg",
            Name = shadowImpEssence.Name,
            Essence = shadowImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var vampireBatEssenceItem = new EssenceItemBase
        {
            Id = "vampireBatId",
            IconPath = "essence-item.svg",
            Name = vampireBatEssence.Name,
            Essence = vampireBatEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var blueSlimeEssenceItem = new EssenceItemBase
        {
            Id = "blueSlimeId",
            IconPath = "essence-item.svg",
            Name = blueSlimeEssence.Name,
            Essence = blueSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var brownSlimeEssenceItem = new EssenceItemBase
        {
            Id = "brownSlimeId",
            IconPath = "essence-item.svg",
            Name = brownSlimeEssence.Name,
            Essence = brownSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var greenSlimeEssenceItem = new EssenceItemBase
        {
            Id = "greenSlimeId",
            IconPath = "essence-item.svg",
            Name = greenSlimeEssence.Name,
            Essence = greenSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var rainbowSlimeEssenceItem = new EssenceItemBase
        {
            Id = "rainbowSlimeId",
            IconPath = "essence-item.svg",
            Name = rainbowSlimeEssence.Name,
            Essence = rainbowSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var redSlimeEssenceItem = new EssenceItemBase
        {
            Id = "redSlimeId",
            IconPath = "essence-item.svg",
            Name = redSlimeEssence.Name,
            Essence = redSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var transparentSlimeEssenceItem = new EssenceItemBase
        {
            Id = "transparentSlimeId",
            IconPath = "essence-item.svg",
            Name = transparentSlimeEssence.Name,
            Essence = transparentSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var enchantedFairyEssenceItem = new EssenceItemBase
        {
            Id = "enchantedFairyId",
            IconPath = "essence-item.svg",
            Name = enchantedFairyEssence.Name,
            Essence = enchantedFairyEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var gladePantherEssenceItem = new EssenceItemBase
        {
            Id = "gladePantherId",
            IconPath = "essence-item.svg",
            Name = gladePantherEssence.Name,
            Essence = gladePantherEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var illusionFoxEssenceItem = new EssenceItemBase
        {
            Id = "illusionFoxId",
            IconPath = "essence-item.svg",
            Name = illusionFoxEssence.Name,
            Essence = illusionFoxEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var nightshadeBlossomEssenceItem = new EssenceItemBase
        {
            Id = "nightshadeBlossomId",
            IconPath = "essence-item.svg",
            Name = nightshadeBlossomEssence.Name,
            Essence = nightshadeBlossomEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var pixieEssenceItem = new EssenceItemBase
        {
            Id = "pixieId",
            IconPath = "essence-item.svg",
            Name = pixieEssence.Name,
            Essence = pixieEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var hobgoblinEssenceItem = new EssenceItemBase
        {
            Id = "hobgoblinId",
            IconPath = "essence-item.svg",
            Name = hobgoblinEssence.Name,
            Essence = hobgoblinEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };

        // Step 4 - Loot Tables
        var goblinEssenceLootTableItem = new LootTableItem { ItemId = goblinEssenceItem.Id, Weight = 25 };
        var goblinWarriorEssenceLootTableItem = new LootTableItem { ItemId = goblinWarriorEssenceItem.Id, Weight = 25 };
        var goblinArcherEssenceLootTableItem = new LootTableItem { ItemId = goblinArcherEssenceItem.Id, Weight = 25 };
        var largeRatEssenceLootTableItem = new LootTableItem { ItemId = largeRatEssenceItem.Id, Weight = 25 };
        var flameImpEssenceLootTableItem = new LootTableItem { ItemId = flameImpEssenceItem.Id, Weight = 25 };
        var frostImpEssenceLootTableItem = new LootTableItem { ItemId = frostImpEssenceItem.Id, Weight = 25 };
        var shadowImpEssenceLootTableItem = new LootTableItem { ItemId = shadowImpEssenceItem.Id, Weight = 25 };
        var vampireBatEssenceLootTableItem = new LootTableItem { ItemId = vampireBatEssenceItem.Id, Weight = 25 };
        var blueSlimeEssenceLootTableItem = new LootTableItem { ItemId = blueSlimeEssenceItem.Id, Weight = 25 };
        var brownSlimeEssenceLootTableItem = new LootTableItem { ItemId = brownSlimeEssenceItem.Id, Weight = 25 };
        var greenSlimeEssenceLootTableItem = new LootTableItem { ItemId = greenSlimeEssenceItem.Id, Weight = 25 };
        var rainbowSlimeEssenceLootTableItem = new LootTableItem { ItemId = rainbowSlimeEssenceItem.Id, Weight = 25 };
        var redSlimeEssenceLootTableItem = new LootTableItem { ItemId = redSlimeEssenceItem.Id, Weight = 25 };
        var transparentSlimeEssenceLootTableItem = new LootTableItem { ItemId = transparentSlimeEssenceItem.Id, Weight = 25 };
        var enchantedFairyEssenceLootTableItem = new LootTableItem { ItemId = enchantedFairyEssenceItem.Id, Weight = 25 };
        var gladePantherEssenceLootTableItem = new LootTableItem { ItemId = gladePantherEssenceItem.Id, Weight = 25 };
        var illusionFoxEssenceLootTableItem = new LootTableItem { ItemId = illusionFoxEssenceItem.Id, Weight = 25 };
        var nightshadeBlossomEssenceLootTableItem = new LootTableItem { ItemId = nightshadeBlossomEssenceItem.Id, Weight = 25 };
        var pixieEssenceLootTableItem = new LootTableItem { ItemId = pixieEssenceItem.Id, Weight = 25 };
        var hobgoblinEssenceLootTableItem = new LootTableItem { ItemId = hobgoblinEssenceItem.Id, Weight = 25 };


        // Create LootTableRarities for Goblin
        var goblinLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [goblinEssenceLootTableItem],
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.01%
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
            Weight = 15 // 0.01%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
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
            Weight = 15 // 0.02%
        };
        var transparentSlimeLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [transparentSlimeLootTableLegendary]
        };
        // Create LootTableRarities for Enchanted Fairy
        var enchantedFairyLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [enchantedFairyEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var enchantedFairyLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [enchantedFairyLootTableLegendary]
        };
        // Create LootTableRarities for Glade Panther
        var gladePantherLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [gladePantherEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var gladePantherLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [gladePantherLootTableLegendary]
        };
        // Create LootTableRarities for Illusion Fox
        var illusionFoxLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [illusionFoxEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var illusionFoxLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [illusionFoxLootTableLegendary]
        };
        // Create LootTableRarities for Nightshade Blossom
        var nightshadeBlossomLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [nightshadeBlossomEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var nightshadeBlossomLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [nightshadeBlossomLootTableLegendary]
        };
        // Create LootTableRarities for Pixie
        var pixieLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [pixieEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var pixieLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [pixieLootTableLegendary]
        };
        // Create LootTableRarities for Hobgoblin
        var hobgoblinLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [hobgoblinEssenceLootTableItem],
            Weight = 15 // 0.02%
        };
        var hobgoblinLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [hobgoblinLootTableLegendary]
        };


        await context.ItemBases.AddRangeAsync(goblinEssenceItem, goblinWarriorEssenceItem, goblinArcherEssenceItem, largeRatEssenceItem);
        await context.ItemBases.AddRangeAsync(flameImpEssenceItem, frostImpEssenceItem, shadowImpEssenceItem, vampireBatEssenceItem);
        await context.ItemBases.AddRangeAsync(blueSlimeEssenceItem, brownSlimeEssenceItem, greenSlimeEssenceItem, rainbowSlimeEssenceItem, redSlimeEssenceItem, transparentSlimeEssenceItem);
        await context.ItemBases.AddRangeAsync(enchantedFairyEssenceItem, gladePantherEssenceItem, illusionFoxEssenceItem, nightshadeBlossomEssenceItem, pixieEssenceItem);
        await context.ItemBases.AddRangeAsync(hobgoblinEssenceItem);
        await context.Essences.AddRangeAsync(goblinEssence, goblinWarriorEssence, goblinArcherEssence, largeRatEssence);
        await context.Essences.AddRangeAsync(flameImpEssence, frostImpEssence, shadowImpEssence, vampireBatEssence);
        await context.Essences.AddRangeAsync(blueSlimeEssence, brownSlimeEssence, greenSlimeEssence, rainbowSlimeEssence, redSlimeEssence, transparentSlimeEssence);
        await context.Essences.AddRangeAsync(enchantedFairyEssence, gladePantherEssence, illusionFoxEssence, nightshadeBlossomEssence, pixieEssence);
        await context.Essences.AddRangeAsync(hobgoblinEssence);
        await context.LootTables.AddRangeAsync(goblinLootTable, goblinWarriorLootTable, goblinArcherLootTable, largeRatLootTable);
        await context.LootTables.AddRangeAsync(flameImpLootTable, frostImpLootTable, shadowImpLootTable, vampireBatLootTable);
        await context.LootTables.AddRangeAsync(blueSlimeLootTable, brownSlimeLootTable, greenSlimeLootTable, rainbowSlimeLootTable, redSlimeLootTable, transparentSlimeLootTable);
        await context.LootTables.AddRangeAsync(enchantedFairyLootTable, gladePantherLootTable, illusionFoxLootTable, nightshadeBlossomLootTable, pixieLootTable);
        await context.LootTables.AddRangeAsync(hobgoblinLootTable);

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
        var enchantedFairyEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = enchantedFairyEssence,
            EntityId = enchantedFairyId
        };
        var gladePantherEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = gladePantherEssence,
            EntityId = gladePantherId
        };
        var illusionFoxEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = illusionFoxEssence,
            EntityId = illusionFoxId
        };
        var nightshadeBlossomEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = nightshadeBlossomEssence,
            EntityId = nightshadeBlossomId
        };
        var pixieEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = pixieEssence,
            EntityId = pixieId
        };
        var hobgoblinEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = hobgoblinEssence,
            EntityId = hobgoblinId
        };

        // Step 5 - Create creatures
        var lumoRuinsCreatures = new List<Creature>
        {
            new() { Id = goblinId, Name = "Goblin", ImagePath = "goblin", LootTableId = goblinLootTable.Id, EssenceSlots = [goblinEssenceSlot], ExperienceReward = 2 },
            new() { Id = goblinWarriorId, Name = "Goblin Warrior", ImagePath = "goblin_warrior", LootTableId = goblinWarriorLootTable.Id, EssenceSlots = [goblinWarriorEssenceSlot], ExperienceReward = 3 },
            new() { Id = goblinArcherId, Name = "Goblin Archer", ImagePath = "goblin_archer", LootTableId = goblinArcherLootTable.Id, EssenceSlots = [goblinArcherEssenceSlot], ExperienceReward = 3 },
            new() { Id = largeRatId, Name = "Large Rat", ImagePath = "large_rat", LootTableId = largeRatLootTable.Id, EssenceSlots = [largeRatEssenceSlot], ExperienceReward = 2 }
        };

        var bloodGroveCreatures = new List<Creature>
        {
            new() { Id = flameImpId, Name = "Flame Imp", ImagePath = "flame_imp", LootTableId = flameImpLootTable.Id, EssenceSlots = [flameImpEssenceSlot], ExperienceReward = 2 },
            new() { Id = frostImpId, Name = "Frost Imp", ImagePath = "frost_imp", LootTableId = frostImpLootTable.Id, EssenceSlots = [frostImpEssenceSlot], ExperienceReward = 2 },
            new() { Id = shadowImpId, Name = "Shadow Imp", ImagePath = "shadow_imp", LootTableId = shadowImpLootTable.Id, EssenceSlots = [shadowImpEssenceSlot], ExperienceReward = 2 },
            new() { Id = vampireBatId, Name = "Vampire Bat", ImagePath = "vampire_bat", LootTableId = vampireBatLootTable.Id, EssenceSlots = [vampireBatEssenceSlot], ExperienceReward = 4 }
        };

        var crystalCreekCreatures = new List<Creature>
        {
            new() { Id = blueSlimeId, Name = "Blue Slime", ImagePath = "blue_slime", LootTableId = blueSlimeLootTable.Id, EssenceSlots = [blueSlimeEssenceSlot], ExperienceReward = 3 },
            new() { Id = brownSlimeId, Name = "Brown Slime", ImagePath = "brown_slime", LootTableId = brownSlimeLootTable.Id, EssenceSlots = [brownSlimeEssenceSlot], ExperienceReward = 4 },
            new() { Id = greenSlimeId, Name = "Green Slime", ImagePath = "green_slime", LootTableId = greenSlimeLootTable.Id, EssenceSlots = [greenSlimeEssenceSlot], ExperienceReward = 3 },
            new() { Id = rainbowSlimeId, Name = "Rainbow Slime", ImagePath = "rainbow_slime", LootTableId = rainbowSlimeLootTable.Id, EssenceSlots = [rainbowSlimeEssenceSlot], ExperienceReward = 4 },
            new() { Id = redSlimeId, Name = "Red Slime", ImagePath = "red_slime", LootTableId = redSlimeLootTable.Id, EssenceSlots = [redSlimeEssenceSlot], ExperienceReward = 3 },
            new() { Id = transparentSlimeId, Name = "Transparent Slime", ImagePath = "transparent_slime", LootTableId = transparentSlimeLootTable.Id, EssenceSlots = [transparentSlimeEssenceSlot], ExperienceReward = 4 },
        };

        var twilightClearingCreatures = new List<Creature>
        {
            new() { Id = enchantedFairyId, Name = "Enchanted Fairy", ImagePath = "enchanted_fairy", LootTableId = enchantedFairyLootTable.Id, EssenceSlots = [enchantedFairyEssenceSlot], ExperienceReward = 6 },
            new() { Id = gladePantherId, Name = "Glade Panther", ImagePath = "glade_panther", LootTableId = gladePantherLootTable.Id, EssenceSlots = [gladePantherEssenceSlot], ExperienceReward = 6 },
            new() { Id = illusionFoxId, Name = "Illusion Fox", ImagePath = "illusion_fox", LootTableId = illusionFoxLootTable.Id, EssenceSlots = [illusionFoxEssenceSlot], ExperienceReward = 7 },
            new() { Id = nightshadeBlossomId, Name = "Nightshade Blossom", ImagePath = "nightshade_blossom", LootTableId = nightshadeBlossomLootTable.Id, EssenceSlots = [nightshadeBlossomEssenceSlot], ExperienceReward = 5 },
            new() { Id = pixieId, Name = "Pixie", ImagePath = "pixie", LootTableId = pixieLootTable.Id, EssenceSlots = [pixieEssenceSlot], ExperienceReward = 6 },
        };

        var goblinMinesCreatures = new List<Creature>
        {
            new() { Id = hobgoblinId, Name = "Hobgoblin", ImagePath = "hobgoblin", LootTableId = hobgoblinLootTable.Id, EssenceSlots = [hobgoblinEssenceSlot], ExperienceReward = 20 },
        };

        await context.Creatures.AddRangeAsync(lumoRuinsCreatures);
        await context.Creatures.AddRangeAsync(bloodGroveCreatures);
        await context.Creatures.AddRangeAsync(crystalCreekCreatures);
        await context.Creatures.AddRangeAsync(twilightClearingCreatures);
        await context.Creatures.AddRangeAsync(goblinMinesCreatures);

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

        var twilightClearingAreaId = "region_01_area_04";
        var twilightClearingAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = twilightClearingAreaId, CreatureId = enchantedFairyId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = twilightClearingAreaId, CreatureId = gladePantherId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = twilightClearingAreaId, CreatureId = illusionFoxId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = twilightClearingAreaId, CreatureId = nightshadeBlossomId, WeightedSpawnRate = 0.10f },
            new AreaCreature() { AreaId = twilightClearingAreaId, CreatureId = pixieId, WeightedSpawnRate = 0.20f },
        };

        var goblinMinesAreaId = "region_01_area_05";
        var goblinMinesAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = goblinMinesAreaId, CreatureId = hobgoblinId, WeightedSpawnRate = 1f },
        };
        // Create attributes
        var attributes = new List<EntityAttribute>();
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinId, -0.3f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinWarriorId, -0.1f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinArcherId, -0.2f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(largeRatId, -0.4f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(flameImpId, 0.1f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(frostImpId, 0.1f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(shadowImpId, 0.1f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(vampireBatId, 0.5f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(blueSlimeId, 0.2f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(brownSlimeId, 0.3f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(greenSlimeId, 0.3f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(rainbowSlimeId, 0.4f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(redSlimeId, 0.3f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(transparentSlimeId, 0.4f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(enchantedFairyId, 0.7f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(gladePantherId, 1f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(illusionFoxId, 0.8f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(nightshadeBlossomId, 0.6f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(pixieId, 0.8f));
        attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(hobgoblinId, 3f));
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
                },
                new Area
                {
                    Id = twilightClearingAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Twilight Clearing",
                    Creatures = twilightClearingAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.75f,
                        0.17f,
                        0.05f,
                        0.02f,
                        0.01f,
                    }
                },
                new Area
                {
                    Id = goblinMinesAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Goblin Mines",
                    Creatures = goblinMinesAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        1f,
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
