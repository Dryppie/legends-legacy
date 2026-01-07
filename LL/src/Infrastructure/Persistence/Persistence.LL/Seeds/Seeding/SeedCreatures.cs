using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;

namespace Persistence.LL.Seeds.Seeding;
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

        var mossLizardId = Guid.Parse("00000000-0000-0000-0000-000000000021");
        var spiderId = Guid.Parse("00000000-0000-0000-0000-000000000022");
        var treantSaplingId = Guid.Parse("00000000-0000-0000-0000-000000000023");
        var venomousSnakeId = Guid.Parse("00000000-0000-0000-0000-000000000024");
        var viperId = Guid.Parse("00000000-0000-0000-0000-000000000025");

        var feralGhoulId = Guid.Parse("00000000-0000-0000-0000-000000000026");
        var plagueGhoulId = Guid.Parse("00000000-0000-0000-0000-000000000027");
        var ravenousGhoulId = Guid.Parse("00000000-0000-0000-0000-000000000028");
        var skeletonArcherId = Guid.Parse("00000000-0000-0000-0000-000000000029");
        var skeletonMageId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var skeletonWarriorId = Guid.Parse("00000000-0000-0000-0000-000000000031");

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

        var mossLizardEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Moss Lizard's Essence",
            ActiveAbilityId = "mossCamouflage",
            PassiveAbilityId = "lostTail",
        };
        var spiderEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Spider's Essence",
            ActiveAbilityId = "skitteringStrike",
            PassiveAbilityId = "spiderEyes",
        };
        var treantSaplingEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Treant Sapling's Essence",
            ActiveAbilityId = "sproutingSurge",
            PassiveAbilityId = "naturingRoots",
        };
        var venomousSnakeEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Venomous Snake's Essence",
            ActiveAbilityId = "venomousSpit",
            PassiveAbilityId = "toxicHide",
        };
        var viperEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Viper's Essence",
            ActiveAbilityId = "piercingFangs",
            PassiveAbilityId = "potentToxins",
        };
        var feralGhoulEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Feral Ghoul's Essence",
            ActiveAbilityId = "feralPounce",
            PassiveAbilityId = "shreddingClaws",
        };
        var plagueGhoulEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Plague Ghoul's Essence",
            ActiveAbilityId = "plagueSwipe",
            PassiveAbilityId = "pestilentTouch",
        };
        var ravenousGhoulEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Ravenous Ghoul's Essence",
            ActiveAbilityId = "drainingClaws",
            PassiveAbilityId = "vileFeast",
        };
        var skeletonArcherEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Skeleton Archer's Essence",
            ActiveAbilityId = "boneArrow",
            PassiveAbilityId = "piercingArrow",
        };
        var skeletonMageEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Skeleton Mage's Essence",
            ActiveAbilityId = "siphon",
            PassiveAbilityId = "protectiveBoneBarrier",
        };
        var skeletonWarriorEssence = new Essence()
        {
            Id = Guid.NewGuid(),
            Name = "Skeleton Warrior's Essence",
            ActiveAbilityId = "boneShield",
            PassiveAbilityId = "spikedDefense",
        };



        // Step 3 - Essence Items
        var goblinEssenceItem = new EssenceItemBase
        {
            Id = "goblinId",
            Name = goblinEssence.Name,
            Essence = goblinEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var goblinWarriorEssenceItem = new EssenceItemBase
        {
            Id = "goblinWarriorId",
            Name = goblinWarriorEssence.Name,
            Essence = goblinWarriorEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var goblinArcherEssenceItem = new EssenceItemBase
        {
            Id = "goblinArcherId",
            Name = goblinArcherEssence.Name,
            Essence = goblinArcherEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var largeRatEssenceItem = new EssenceItemBase
        {
            Id = "largeRatId",
            Name = largeRatEssence.Name,
            Essence = largeRatEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var flameImpEssenceItem = new EssenceItemBase
        {
            Id = "flameImpId",
            Name = flameImpEssence.Name,
            Essence = flameImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var frostImpEssenceItem = new EssenceItemBase
        {
            Id = "frostImpId",
            Name = frostImpEssence.Name,
            Essence = frostImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var shadowImpEssenceItem = new EssenceItemBase
        {
            Id = "shadowImpId",
            Name = shadowImpEssence.Name,
            Essence = shadowImpEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var vampireBatEssenceItem = new EssenceItemBase
        {
            Id = "vampireBatId",
            Name = vampireBatEssence.Name,
            Essence = vampireBatEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var blueSlimeEssenceItem = new EssenceItemBase
        {
            Id = "blueSlimeId",
            Name = blueSlimeEssence.Name,
            Essence = blueSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var brownSlimeEssenceItem = new EssenceItemBase
        {
            Id = "brownSlimeId",
            Name = brownSlimeEssence.Name,
            Essence = brownSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var greenSlimeEssenceItem = new EssenceItemBase
        {
            Id = "greenSlimeId",
            Name = greenSlimeEssence.Name,
            Essence = greenSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var rainbowSlimeEssenceItem = new EssenceItemBase
        {
            Id = "rainbowSlimeId",
            Name = rainbowSlimeEssence.Name,
            Essence = rainbowSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var redSlimeEssenceItem = new EssenceItemBase
        {
            Id = "redSlimeId",
            Name = redSlimeEssence.Name,
            Essence = redSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var transparentSlimeEssenceItem = new EssenceItemBase
        {
            Id = "transparentSlimeId",
            Name = transparentSlimeEssence.Name,
            Essence = transparentSlimeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var enchantedFairyEssenceItem = new EssenceItemBase
        {
            Id = "enchantedFairyId",
            Name = enchantedFairyEssence.Name,
            Essence = enchantedFairyEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var gladePantherEssenceItem = new EssenceItemBase
        {
            Id = "gladePantherId",
            Name = gladePantherEssence.Name,
            Essence = gladePantherEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var illusionFoxEssenceItem = new EssenceItemBase
        {
            Id = "illusionFoxId",
            Name = illusionFoxEssence.Name,
            Essence = illusionFoxEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var nightshadeBlossomEssenceItem = new EssenceItemBase
        {
            Id = "nightshadeBlossomId",
            Name = nightshadeBlossomEssence.Name,
            Essence = nightshadeBlossomEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var pixieEssenceItem = new EssenceItemBase
        {
            Id = "pixieId",
            Name = pixieEssence.Name,
            Essence = pixieEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var hobgoblinEssenceItem = new EssenceItemBase
        {
            Id = "hobgoblinId",
            Name = hobgoblinEssence.Name,
            Essence = hobgoblinEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var mossLizardEssenceItem = new EssenceItemBase
        {
            Id = "mossLizardId",
            Name = mossLizardEssence.Name,
            Essence = mossLizardEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var spiderEssenceItem = new EssenceItemBase
        {
            Id = "spiderId",
            Name = spiderEssence.Name,
            Essence = spiderEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var treantSaplingEssenceItem = new EssenceItemBase
        {
            Id = "treantSaplingId",
            Name = treantSaplingEssence.Name,
            Essence = treantSaplingEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var venomousSnakeEssenceItem = new EssenceItemBase
        {
            Id = "venomousSnakeId",
            Name = venomousSnakeEssence.Name,
            Essence = venomousSnakeEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var viperEssenceItem = new EssenceItemBase
        {
            Id = "viperId",
            Name = viperEssence.Name,
            Essence = viperEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var feralGhoulEssenceItem = new EssenceItemBase
        {
            Id = "feralGhoulId",
            Name = feralGhoulEssence.Name,
            Essence = feralGhoulEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var plagueGhoulEssenceItem = new EssenceItemBase
        {
            Id = "plagueGhoulId",
            Name = plagueGhoulEssence.Name,
            Essence = plagueGhoulEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var ravenousGhoulEssenceItem = new EssenceItemBase
        {
            Id = "ravenousGhoulId",
            Name = ravenousGhoulEssence.Name,
            Essence = ravenousGhoulEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var skeletonArcherEssenceItem = new EssenceItemBase
        {
            Id = "skeletonArcherId",
            Name = skeletonArcherEssence.Name,
            Essence = skeletonArcherEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var skeletonMageEssenceItem = new EssenceItemBase
        {
            Id = "skeletonMageId",
            Name = skeletonMageEssence.Name,
            Essence = skeletonMageEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };
        var skeletonWarriorEssenceItem = new EssenceItemBase
        {
            Id = "skeletonWarriorId",
            Name = skeletonWarriorEssence.Name,
            Essence = skeletonWarriorEssence,
            ItemType = ItemType.Essence,
            Rarity = Rarity.Unique
        };


        // Step 4 - Loot Tables
        var goblinEssenceLootTableItem = new LootTableItem { ItemId = goblinEssenceItem.Id, Weight = 10 };
        var goblinWarriorEssenceLootTableItem = new LootTableItem { ItemId = goblinWarriorEssenceItem.Id, Weight = 10 };
        var goblinArcherEssenceLootTableItem = new LootTableItem { ItemId = goblinArcherEssenceItem.Id, Weight = 10 };
        var largeRatEssenceLootTableItem = new LootTableItem { ItemId = largeRatEssenceItem.Id, Weight = 10 };
        var flameImpEssenceLootTableItem = new LootTableItem { ItemId = flameImpEssenceItem.Id, Weight = 5 };
        var frostImpEssenceLootTableItem = new LootTableItem { ItemId = frostImpEssenceItem.Id, Weight = 5 };
        var shadowImpEssenceLootTableItem = new LootTableItem { ItemId = shadowImpEssenceItem.Id, Weight = 5 };
        var vampireBatEssenceLootTableItem = new LootTableItem { ItemId = vampireBatEssenceItem.Id, Weight = 5 };
        var blueSlimeEssenceLootTableItem = new LootTableItem { ItemId = blueSlimeEssenceItem.Id, Weight = 5 };
        var brownSlimeEssenceLootTableItem = new LootTableItem { ItemId = brownSlimeEssenceItem.Id, Weight = 5 };
        var greenSlimeEssenceLootTableItem = new LootTableItem { ItemId = greenSlimeEssenceItem.Id, Weight = 5 };
        var rainbowSlimeEssenceLootTableItem = new LootTableItem { ItemId = rainbowSlimeEssenceItem.Id, Weight = 5 };
        var redSlimeEssenceLootTableItem = new LootTableItem { ItemId = redSlimeEssenceItem.Id, Weight = 5 };
        var transparentSlimeEssenceLootTableItem = new LootTableItem { ItemId = transparentSlimeEssenceItem.Id, Weight = 5 };
        var enchantedFairyEssenceLootTableItem = new LootTableItem { ItemId = enchantedFairyEssenceItem.Id, Weight = 5 };
        var gladePantherEssenceLootTableItem = new LootTableItem { ItemId = gladePantherEssenceItem.Id, Weight = 5 };
        var illusionFoxEssenceLootTableItem = new LootTableItem { ItemId = illusionFoxEssenceItem.Id, Weight = 5 };
        var nightshadeBlossomEssenceLootTableItem = new LootTableItem { ItemId = nightshadeBlossomEssenceItem.Id, Weight = 5 };
        var pixieEssenceLootTableItem = new LootTableItem { ItemId = pixieEssenceItem.Id, Weight = 5 };
        var hobgoblinEssenceLootTableItem = new LootTableItem { ItemId = hobgoblinEssenceItem.Id, Weight = 5 };
        var mossLizardEssenceLootTableItem = new LootTableItem { ItemId = mossLizardEssenceItem.Id, Weight = 5 };
        var spiderEssenceLootTableItem = new LootTableItem { ItemId = spiderEssenceItem.Id, Weight = 5 };
        var treantSaplingEssenceLootTableItem = new LootTableItem { ItemId = treantSaplingEssenceItem.Id, Weight = 5 };
        var venomousSnakeEssenceLootTableItem = new LootTableItem { ItemId = venomousSnakeEssenceItem.Id, Weight = 5 };
        var viperEssenceLootTableItem = new LootTableItem { ItemId = viperEssenceItem.Id, Weight = 5 };
        var feralGhoulEssenceLootTableItem = new LootTableItem { ItemId = feralGhoulEssenceItem.Id, Weight = 5 };
        var plagueGhoulEssenceLootTableItem = new LootTableItem { ItemId = plagueGhoulEssenceItem.Id, Weight = 5 };
        var ravenousGhoulEssenceLootTableItem = new LootTableItem { ItemId = ravenousGhoulEssenceItem.Id, Weight = 5 };
        var skeletonArcherEssenceLootTableItem = new LootTableItem { ItemId = skeletonArcherEssenceItem.Id, Weight = 5 };
        var skeletonMageEssenceLootTableItem = new LootTableItem { ItemId = skeletonMageEssenceItem.Id, Weight = 5 };
        var skeletonWarriorEssenceLootTableItem = new LootTableItem { ItemId = skeletonWarriorEssenceItem.Id, Weight = 5 };



        // Create LootTableRarities for Goblin
        var goblinLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [goblinEssenceLootTableItem],
            Weight = 10 // 0.02%
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
            Weight = 10 // 0.01%
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
            Weight = 10 // 0.01%
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
            Weight = 10 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
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
            Weight = 5 // 0.02%
        };
        var hobgoblinLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [hobgoblinLootTableLegendary]
        };
        // Create LootTableRarities for Moss Lizard
        var mossLizardLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [mossLizardEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var mossLizardLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [mossLizardLootTableLegendary]
        };
        // Create LootTableRarities for Spider
        var spiderLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [spiderEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var spiderLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [spiderLootTableLegendary]
        };
        // Create LootTableRarities for Treant Sapling
        var treantSaplingLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [treantSaplingEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var treantSaplingLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [treantSaplingLootTableLegendary]
        };
        // Create LootTableRarities for Venomous Snake
        var venomousSnakeLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [venomousSnakeEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var venomousSnakeLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [venomousSnakeLootTableLegendary]
        };
        // Create LootTableRarities for Viper
        var viperLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [viperEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var viperLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [viperLootTableLegendary]
        };
        // Create LootTableRarities for Feral Ghoul
        var feralGhoulLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [feralGhoulEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var feralGhoulLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [feralGhoulLootTableLegendary]
        };
        // Create LootTableRarities for Plague Ghoul
        var plagueGhoulLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [plagueGhoulEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var plagueGhoulLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [plagueGhoulLootTableLegendary]
        };
        // Create LootTableRarities for Ravenous Ghoul
        var ravenousGhoulLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [ravenousGhoulEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var ravenousGhoulLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [ravenousGhoulLootTableLegendary]
        };
        // Create LootTableRarities for Skeleton Archer
        var skeletonArcherLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonArcherEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var skeletonArcherLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonArcherLootTableLegendary]
        };
        // Create LootTableRarities for Skeleton Mage
        var skeletonMageLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonMageEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var skeletonMageLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonMageLootTableLegendary]
        };
        // Create LootTableRarities for Skeleton Warrior
        var skeletonWarriorLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonWarriorEssenceLootTableItem],
            Weight = 5 // 0.02%
        };
        var skeletonWarriorLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonWarriorLootTableLegendary]
        };

        // Items
        await context.ItemBases.AddRangeAsync(goblinEssenceItem, goblinWarriorEssenceItem, goblinArcherEssenceItem, largeRatEssenceItem);
        await context.ItemBases.AddRangeAsync(flameImpEssenceItem, frostImpEssenceItem, shadowImpEssenceItem, vampireBatEssenceItem);
        await context.ItemBases.AddRangeAsync(blueSlimeEssenceItem, brownSlimeEssenceItem, greenSlimeEssenceItem, rainbowSlimeEssenceItem, redSlimeEssenceItem, transparentSlimeEssenceItem);
        await context.ItemBases.AddRangeAsync(enchantedFairyEssenceItem, gladePantherEssenceItem, illusionFoxEssenceItem, nightshadeBlossomEssenceItem, pixieEssenceItem);
        await context.ItemBases.AddRangeAsync(hobgoblinEssenceItem);
        await context.ItemBases.AddRangeAsync(mossLizardEssenceItem, spiderEssenceItem, treantSaplingEssenceItem, venomousSnakeEssenceItem, viperEssenceItem);
        await context.ItemBases.AddRangeAsync(feralGhoulEssenceItem, plagueGhoulEssenceItem, ravenousGhoulEssenceItem, skeletonArcherEssenceItem, skeletonMageEssenceItem, skeletonWarriorEssenceItem);
        // Essences
        await context.Essences.AddRangeAsync(goblinEssence, goblinWarriorEssence, goblinArcherEssence, largeRatEssence);
        await context.Essences.AddRangeAsync(flameImpEssence, frostImpEssence, shadowImpEssence, vampireBatEssence);
        await context.Essences.AddRangeAsync(blueSlimeEssence, brownSlimeEssence, greenSlimeEssence, rainbowSlimeEssence, redSlimeEssence, transparentSlimeEssence);
        await context.Essences.AddRangeAsync(enchantedFairyEssence, gladePantherEssence, illusionFoxEssence, nightshadeBlossomEssence, pixieEssence);
        await context.Essences.AddRangeAsync(hobgoblinEssence);
        await context.Essences.AddRangeAsync(mossLizardEssence, spiderEssence, treantSaplingEssence, venomousSnakeEssence, viperEssence);
        await context.Essences.AddRangeAsync(feralGhoulEssence, plagueGhoulEssence, ravenousGhoulEssence, skeletonArcherEssence, skeletonMageEssence, skeletonWarriorEssence);
        // Loot tables
        await context.LootTables.AddRangeAsync(goblinLootTable, goblinWarriorLootTable, goblinArcherLootTable, largeRatLootTable);
        await context.LootTables.AddRangeAsync(flameImpLootTable, frostImpLootTable, shadowImpLootTable, vampireBatLootTable);
        await context.LootTables.AddRangeAsync(blueSlimeLootTable, brownSlimeLootTable, greenSlimeLootTable, rainbowSlimeLootTable, redSlimeLootTable, transparentSlimeLootTable);
        await context.LootTables.AddRangeAsync(enchantedFairyLootTable, gladePantherLootTable, illusionFoxLootTable, nightshadeBlossomLootTable, pixieLootTable);
        await context.LootTables.AddRangeAsync(hobgoblinLootTable);
        await context.LootTables.AddRangeAsync(mossLizardLootTable, spiderLootTable, treantSaplingLootTable, venomousSnakeLootTable, viperLootTable);
        await context.LootTables.AddRangeAsync(feralGhoulLootTable, plagueGhoulLootTable, ravenousGhoulLootTable, skeletonArcherLootTable, skeletonMageLootTable, skeletonWarriorLootTable);


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
        var mossLizardEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = mossLizardEssence,
            EntityId = mossLizardId
        };
        var spiderEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = spiderEssence,
            EntityId = spiderId
        };
        var treantSaplingEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = treantSaplingEssence,
            EntityId = treantSaplingId
        };
        var venomousSnakeEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = venomousSnakeEssence,
            EntityId = venomousSnakeId
        };
        var viperEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = viperEssence,
            EntityId = viperId
        };
        var feralGhoulEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = feralGhoulEssence,
            EntityId = feralGhoulId
        };
        var plagueGhoulEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = plagueGhoulEssence,
            EntityId = plagueGhoulId
        };
        var ravenousGhoulEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = ravenousGhoulEssence,
            EntityId = ravenousGhoulId
        };
        var skeletonArcherEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = skeletonArcherEssence,
            EntityId = skeletonArcherId
        };
        var skeletonMageEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = skeletonMageEssence,
            EntityId = skeletonMageId
        };
        var skeletonWarriorEssenceSlot = new EssenceSlot()
        {
            Id = Guid.NewGuid(),
            SlotState = SlotState.Active,
            SlotType = SlotType.Standard,
            OccupiedEssence = skeletonWarriorEssence,
            EntityId = skeletonWarriorId
        };


        // Step 5 - Create creatures
        var lumoRuinsCreatures = new List<Creature>
        {
            new() { Id = goblinId, Name = "Goblin", ImagePath = "goblin", LootTableId = goblinLootTable.Id, EssenceSlots = [goblinEssenceSlot], ExperienceReward = 4 },
            new() { Id = goblinWarriorId, Name = "Goblin Warrior", ImagePath = "goblin_warrior", LootTableId = goblinWarriorLootTable.Id, EssenceSlots = [goblinWarriorEssenceSlot], ExperienceReward = 6, Archetype = CreatureArchetype.Bruiser },
            new() { Id = goblinArcherId, Name = "Goblin Archer", ImagePath = "goblin_archer", LootTableId = goblinArcherLootTable.Id, EssenceSlots = [goblinArcherEssenceSlot], ExperienceReward = 5, Archetype = CreatureArchetype.DPS },
            new() { Id = largeRatId, Name = "Large Rat", ImagePath = "large_rat", LootTableId = largeRatLootTable.Id, EssenceSlots = [largeRatEssenceSlot], ExperienceReward = 5, Archetype = CreatureArchetype.Tank }
        };

        var bloodGroveCreatures = new List<Creature>
        {
            new() { Id = flameImpId, Name = "Flame Imp", ImagePath = "flame_imp", LootTableId = flameImpLootTable.Id, EssenceSlots = [flameImpEssenceSlot], ExperienceReward = 6 },
            new() { Id = frostImpId, Name = "Frost Imp", ImagePath = "frost_imp", LootTableId = frostImpLootTable.Id, EssenceSlots = [frostImpEssenceSlot], ExperienceReward = 6 },
            new() { Id = shadowImpId, Name = "Shadow Imp", ImagePath = "shadow_imp", LootTableId = shadowImpLootTable.Id, EssenceSlots = [shadowImpEssenceSlot], ExperienceReward = 6 },
            new() { Id = vampireBatId, Name = "Vampire Bat", ImagePath = "vampire_bat", LootTableId = vampireBatLootTable.Id, EssenceSlots = [vampireBatEssenceSlot], ExperienceReward = 8 }
        };

        var crystalCreekCreatures = new List<Creature>
        {
            new() { Id = blueSlimeId, Name = "Blue Slime", ImagePath = "blue_slime", LootTableId = blueSlimeLootTable.Id, EssenceSlots = [blueSlimeEssenceSlot], ExperienceReward = 10 },
            new() { Id = brownSlimeId, Name = "Brown Slime", ImagePath = "brown_slime", LootTableId = brownSlimeLootTable.Id, EssenceSlots = [brownSlimeEssenceSlot], ExperienceReward = 10 },
            new() { Id = greenSlimeId, Name = "Green Slime", ImagePath = "green_slime", LootTableId = greenSlimeLootTable.Id, EssenceSlots = [greenSlimeEssenceSlot], ExperienceReward = 10 },
            new() { Id = rainbowSlimeId, Name = "Rainbow Slime", ImagePath = "rainbow_slime", LootTableId = rainbowSlimeLootTable.Id, EssenceSlots = [rainbowSlimeEssenceSlot], ExperienceReward = 12 },
            new() { Id = redSlimeId, Name = "Red Slime", ImagePath = "red_slime", LootTableId = redSlimeLootTable.Id, EssenceSlots = [redSlimeEssenceSlot], ExperienceReward = 10 },
            new() { Id = transparentSlimeId, Name = "Transparent Slime", ImagePath = "transparent_slime", LootTableId = transparentSlimeLootTable.Id, EssenceSlots = [transparentSlimeEssenceSlot], ExperienceReward = 11 },
        };

        var twilightClearingCreatures = new List<Creature>
        {
            new() { Id = enchantedFairyId, Name = "Enchanted Fairy", ImagePath = "enchanted_fairy", LootTableId = enchantedFairyLootTable.Id, EssenceSlots = [enchantedFairyEssenceSlot], ExperienceReward = 14 },
            new() { Id = gladePantherId, Name = "Glade Panther", ImagePath = "glade_panther", LootTableId = gladePantherLootTable.Id, EssenceSlots = [gladePantherEssenceSlot], ExperienceReward = 14 },
            new() { Id = illusionFoxId, Name = "Illusion Fox", ImagePath = "illusion_fox", LootTableId = illusionFoxLootTable.Id, EssenceSlots = [illusionFoxEssenceSlot], ExperienceReward = 14 },
            new() { Id = nightshadeBlossomId, Name = "Nightshade Blossom", ImagePath = "nightshade_blossom", LootTableId = nightshadeBlossomLootTable.Id, EssenceSlots = [nightshadeBlossomEssenceSlot], ExperienceReward = 15 },
            new() { Id = pixieId, Name = "Pixie", ImagePath = "pixie", LootTableId = pixieLootTable.Id, EssenceSlots = [pixieEssenceSlot], ExperienceReward = 12 },
        };

        var goblinMinesCreatures = new List<Creature>
        {
            new() { Id = hobgoblinId, Name = "Hobgoblin", ImagePath = "hobgoblin", LootTableId = hobgoblinLootTable.Id, EssenceSlots = [hobgoblinEssenceSlot], ExperienceReward = 20 },
        };

        var oakThicketCreatures = new List<Creature>
        {
            new() { Id = mossLizardId, Name = "Moss Lizard", ImagePath = "moss_lizard", LootTableId = mossLizardLootTable.Id, EssenceSlots = [mossLizardEssenceSlot], ExperienceReward = 8 },
            new() { Id = spiderId, Name = "Spider", ImagePath = "spider", LootTableId = spiderLootTable.Id, EssenceSlots = [spiderEssenceSlot], ExperienceReward = 9 },
            new() { Id = treantSaplingId, Name = "Treant Sapling", ImagePath = "treant_sapling", LootTableId = treantSaplingLootTable.Id, EssenceSlots = [treantSaplingEssenceSlot], ExperienceReward = 10 },
            new() { Id = venomousSnakeId, Name = "Venomous Snake", ImagePath = "venomous_snake", LootTableId = venomousSnakeLootTable.Id, EssenceSlots = [venomousSnakeEssenceSlot], ExperienceReward = 11 },
            new() { Id = viperId, Name = "Viper", ImagePath = "viper", LootTableId = viperLootTable.Id, EssenceSlots = [viperEssenceSlot], ExperienceReward = 12 },
        };

        var forgottenRuinsCreatures = new List<Creature>
        {
            new() { Id = feralGhoulId, Name = "Feral Ghoul", ImagePath = "feral_ghoul", LootTableId = feralGhoulLootTable.Id, EssenceSlots = [feralGhoulEssenceSlot], ExperienceReward = 15 },
            new() { Id = plagueGhoulId, Name = "Plague Ghoul", ImagePath = "plague_ghoul", LootTableId = plagueGhoulLootTable.Id, EssenceSlots = [plagueGhoulEssenceSlot], ExperienceReward = 16 },
            new() { Id = ravenousGhoulId, Name = "Ravenous Ghoul", ImagePath = "ravenous_ghoul", LootTableId = ravenousGhoulLootTable.Id, EssenceSlots = [ravenousGhoulEssenceSlot], ExperienceReward = 17 },
            new() { Id = skeletonArcherId, Name = "Skeleton Archer", ImagePath = "skeleton_archer", LootTableId = skeletonArcherLootTable.Id, EssenceSlots = [skeletonArcherEssenceSlot], ExperienceReward = 18 },
            new() { Id = skeletonMageId, Name = "Skeleton Mage", ImagePath = "skeleton_mage", LootTableId = skeletonMageLootTable.Id, EssenceSlots = [skeletonMageEssenceSlot], ExperienceReward = 19 },
            new() { Id = skeletonWarriorId, Name = "Skeleton Warrior", ImagePath = "skeleton_warrior", LootTableId = skeletonWarriorLootTable.Id, EssenceSlots = [skeletonWarriorEssenceSlot], ExperienceReward = 20 },
        };

        await context.Creatures.AddRangeAsync(lumoRuinsCreatures);
        await context.Creatures.AddRangeAsync(bloodGroveCreatures);
        await context.Creatures.AddRangeAsync(crystalCreekCreatures);
        await context.Creatures.AddRangeAsync(twilightClearingCreatures);
        await context.Creatures.AddRangeAsync(goblinMinesCreatures);
        await context.Creatures.AddRangeAsync(oakThicketCreatures);
        await context.Creatures.AddRangeAsync(forgottenRuinsCreatures);


        // Step 6 - Create area
        var lumoRuinsAreaId = "region_01_area_01";
        var lumoRuinsAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinId, WeightedSpawnRate = 0.70f },
            new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinWarriorId, WeightedSpawnRate = 0.07f },
            new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = goblinArcherId, WeightedSpawnRate = 0.08f },
            new AreaCreature() { AreaId = lumoRuinsAreaId, CreatureId = largeRatId, WeightedSpawnRate = 0.15f },
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

        var oakThicketAreaId = "region_01_area_06";
        var oakThicketAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = oakThicketAreaId, CreatureId = mossLizardId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = oakThicketAreaId, CreatureId = spiderId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = oakThicketAreaId, CreatureId = treantSaplingId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = oakThicketAreaId, CreatureId = venomousSnakeId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = oakThicketAreaId, CreatureId = viperId, WeightedSpawnRate = 0.20f },
        };

        var forgottenRuinsAreaId = "region_01_area_07";
        var forgottenRuinsAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = feralGhoulId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = plagueGhoulId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = ravenousGhoulId, WeightedSpawnRate = 0.10f },
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = skeletonArcherId, WeightedSpawnRate = 0.15f },
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = skeletonMageId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = forgottenRuinsAreaId, CreatureId = skeletonWarriorId, WeightedSpawnRate = 0.15f },
        };

        //// Create attributes
        //var attributes = new List<EntityAttribute>();
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinId, -0.5f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinWarriorId, -0.1f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(goblinArcherId, -0.2f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(largeRatId, -0.7f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(flameImpId, 0.3f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(frostImpId, 0.6f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(shadowImpId, 0.4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(vampireBatId, 0.9f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(blueSlimeId, 1.5f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(brownSlimeId, 1.2f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(greenSlimeId, 1.1f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(rainbowSlimeId, 1.4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(redSlimeId, 1.3f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(transparentSlimeId, 1.4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(enchantedFairyId, 1.6f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(gladePantherId, 1.8f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(illusionFoxId, 1.8f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(nightshadeBlossomId, 2f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(pixieId, 1.4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(hobgoblinId, 4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(mossLizardId, 2.1f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(spiderId, 1.9f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(treantSaplingId, 2f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(venomousSnakeId, 1.9f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(viperId, 1.7f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(feralGhoulId, 2.4f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(plagueGhoulId, 2.3f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(ravenousGhoulId, 2.7f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(skeletonArcherId, 2.8f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(skeletonMageId, 2.9f));
        //attributes.AddRange(EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(skeletonWarriorId, 2.6f));

        //await context.EntityAttributes.AddRangeAsync(attributes);

        if (!context.Regions.Any())
        {
            var areas = new List<Area>()
            {
                new Area
                {
                    Id = lumoRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Lumo Ruins",
                    LevelRequirement = 1,
                    Creatures = lumoRuinsAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.969f,
                        0.03f,
                        0.001f,
                    }
                },
                new Area
                {
                    Id = bloodGroveAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Blood Grove",
                    LevelRequirement = 5,
                    Creatures = bloodGroveAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.03f,
                        0.969f,
                        0.001f,
                    }
                },
                new Area
                {
                    Id = crystalCreekAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Crystal Creek",
                    LevelRequirement = 10,
                    Creatures = crystalCreekAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.03f,
                        0.969f,
                        0.001f,
                    }
                },
                new Area
                {
                    Id = twilightClearingAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Twilight Clearing",
                    LevelRequirement = 15,
                    Creatures = twilightClearingAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.03f,
                        0.969f,
                        0.001f,
                    }
                },
                new Area
                {
                    Id = goblinMinesAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Goblin Mines",
                    LevelRequirement = 20,
                    Creatures = goblinMinesAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        1f,
                    }
                },
                new Area
                {
                    Id = oakThicketAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Oak Thicket",
                    LevelRequirement = 25,
                    Creatures = oakThicketAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.03f,
                        0.969f,
                        0.001f,
                    }
                },
                new Area
                {
                    Id = forgottenRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Forgotten Ruins",
                    LevelRequirement = 30,
                    Creatures = forgottenRuinsAreaCreatures,
                    SpawnProbabilities = new List<float>
                    {
                        0.03f,
                        0.969f,
                        0.001f,
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
