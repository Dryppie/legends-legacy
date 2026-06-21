using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Domain.Models.Professions.Gathering.GatheringNodes;
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
        var antWorkerId = Guid.Parse("00000000-0000-0000-0000-000000000032");
        var fireAntId = Guid.Parse("00000000-0000-0000-0000-000000000033");
        var forestSpiritId = Guid.Parse("00000000-0000-0000-0000-000000000034");
        var woodNymphId = Guid.Parse("00000000-0000-0000-0000-000000000035");
        var giantSpiderId = Guid.Parse("00000000-0000-0000-0000-000000000036");
        var venomousSpiderlingId = Guid.Parse("00000000-0000-0000-0000-000000000037");
        // Step 2 - Loot Tables


        // Create LootTableRarities for Goblin
        var goblinLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
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
            Entries = [],
            Weight = 5 // 0.02%
        };
        var skeletonWarriorLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [skeletonWarriorLootTableLegendary]
        };
        // Create LootTableRarities for Ant Worker
        var antWorkerLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var antWorkerLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [antWorkerLootTableLegendary]
        };
        // Create LootTableRarities for Fire Ant
        var fireAntLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var fireAntLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [fireAntLootTableLegendary]
        };
        // Create LootTableRarities for Forest Spirit
        var forestSpiritLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var forestSpiritLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [forestSpiritLootTableLegendary]
        };
        // Create LootTableRarities for Wood Nymph
        var woodNymphLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var woodNymphLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [woodNymphLootTableLegendary]
        };
        // Create LootTableRarities for Giant Spider
        var giantSpiderLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var giantSpiderLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [giantSpiderLootTableLegendary]
        };
        // Create LootTableRarities for Venomous Spiderling
        var venomousSpiderlingLootTableLegendary = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [],
            Weight = 5 // 0.02%
        };
        var venomousSpiderlingLootTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [venomousSpiderlingLootTableLegendary]
        };
        // Loot tables
        await context.LootTables.AddRangeAsync(goblinLootTable, goblinWarriorLootTable, goblinArcherLootTable, largeRatLootTable);
        await context.LootTables.AddRangeAsync(flameImpLootTable, frostImpLootTable, shadowImpLootTable, vampireBatLootTable);
        await context.LootTables.AddRangeAsync(blueSlimeLootTable, brownSlimeLootTable, greenSlimeLootTable, rainbowSlimeLootTable, redSlimeLootTable, transparentSlimeLootTable);
        await context.LootTables.AddRangeAsync(enchantedFairyLootTable, gladePantherLootTable, illusionFoxLootTable, nightshadeBlossomLootTable, pixieLootTable);
        await context.LootTables.AddRangeAsync(hobgoblinLootTable);
        await context.LootTables.AddRangeAsync(mossLizardLootTable, spiderLootTable, treantSaplingLootTable, venomousSnakeLootTable, viperLootTable);
        await context.LootTables.AddRangeAsync(feralGhoulLootTable, plagueGhoulLootTable, ravenousGhoulLootTable, skeletonArcherLootTable, skeletonMageLootTable, skeletonWarriorLootTable);
        await context.LootTables.AddRangeAsync(antWorkerLootTable, fireAntLootTable, forestSpiritLootTable, woodNymphLootTable, giantSpiderLootTable, venomousSpiderlingLootTable);

        // Step 5 - Create creatures
        var lumoRuinsCreatures = new List<Creature>
        {
            new() { Id = goblinId, Name = "Goblin", ImagePath = "goblin", LootTableId = goblinLootTable.Id, ExperienceReward = 4 },
            new() { Id = goblinWarriorId, Name = "Goblin Warrior", ImagePath = "goblin_warrior", LootTableId = goblinWarriorLootTable.Id, ExperienceReward = 6, Archetype = CreatureArchetype.Bruiser },
            new() { Id = goblinArcherId, Name = "Goblin Archer", ImagePath = "goblin_archer", LootTableId = goblinArcherLootTable.Id, ExperienceReward = 5, Archetype = CreatureArchetype.DPS },
            new() { Id = largeRatId, Name = "Large Rat", ImagePath = "large_rat", LootTableId = largeRatLootTable.Id, ExperienceReward = 5, Archetype = CreatureArchetype.Tank }
        };

        var bloodGroveCreatures = new List<Creature>
        {
            new() { Id = flameImpId, Name = "Flame Imp", ImagePath = "flame_imp", LootTableId = flameImpLootTable.Id, ExperienceReward = 6 },
            new() { Id = frostImpId, Name = "Frost Imp", ImagePath = "frost_imp", LootTableId = frostImpLootTable.Id, ExperienceReward = 6 },
            new() { Id = shadowImpId, Name = "Shadow Imp", ImagePath = "shadow_imp", LootTableId = shadowImpLootTable.Id, ExperienceReward = 6 },
            new() { Id = vampireBatId, Name = "Vampire Bat", ImagePath = "vampire_bat", LootTableId = vampireBatLootTable.Id, ExperienceReward = 8 }
        };

        var crystalCreekCreatures = new List<Creature>
        {
            new() { Id = blueSlimeId, Name = "Blue Slime", ImagePath = "blue_slime", LootTableId = blueSlimeLootTable.Id, ExperienceReward = 10 },
            new() { Id = brownSlimeId, Name = "Brown Slime", ImagePath = "brown_slime", LootTableId = brownSlimeLootTable.Id, ExperienceReward = 10 },
            new() { Id = greenSlimeId, Name = "Green Slime", ImagePath = "green_slime", LootTableId = greenSlimeLootTable.Id, ExperienceReward = 10 },
            new() { Id = rainbowSlimeId, Name = "Rainbow Slime", ImagePath = "rainbow_slime", LootTableId = rainbowSlimeLootTable.Id, ExperienceReward = 12 },
            new() { Id = redSlimeId, Name = "Red Slime", ImagePath = "red_slime", LootTableId = redSlimeLootTable.Id, ExperienceReward = 10 },
            new() { Id = transparentSlimeId, Name = "Transparent Slime", ImagePath = "transparent_slime", LootTableId = transparentSlimeLootTable.Id, ExperienceReward = 11 },
        };

        var twilightClearingCreatures = new List<Creature>
        {
            new() { Id = enchantedFairyId, Name = "Enchanted Fairy", ImagePath = "enchanted_fairy", LootTableId = enchantedFairyLootTable.Id, ExperienceReward = 14 },
            new() { Id = gladePantherId, Name = "Glade Panther", ImagePath = "glade_panther", LootTableId = gladePantherLootTable.Id, ExperienceReward = 14 },
            new() { Id = illusionFoxId, Name = "Illusion Fox", ImagePath = "illusion_fox", LootTableId = illusionFoxLootTable.Id, ExperienceReward = 14 },
            new() { Id = nightshadeBlossomId, Name = "Nightshade Blossom", ImagePath = "nightshade_blossom", LootTableId = nightshadeBlossomLootTable.Id, ExperienceReward = 15 },
            new() { Id = pixieId, Name = "Pixie", ImagePath = "pixie", LootTableId = pixieLootTable.Id, ExperienceReward = 12 },
        };

        var goblinMinesDungeonCreatures = new List<Creature>
        {
            new() { Id = hobgoblinId, Name = "Hobgoblin", ImagePath = "hobgoblin", LootTableId = hobgoblinLootTable.Id, ExperienceReward = 20 },
        };

        var oakThicketCreatures = new List<Creature>
        {
            new() { Id = mossLizardId, Name = "Moss Lizard", ImagePath = "moss_lizard", LootTableId = mossLizardLootTable.Id, ExperienceReward = 8 },
            new() { Id = spiderId, Name = "Spider", ImagePath = "spider", LootTableId = spiderLootTable.Id, ExperienceReward = 9 },
            new() { Id = treantSaplingId, Name = "Treant Sapling", ImagePath = "treant_sapling", LootTableId = treantSaplingLootTable.Id, ExperienceReward = 10 },
            new() { Id = venomousSnakeId, Name = "Venomous Snake", ImagePath = "venomous_snake", LootTableId = venomousSnakeLootTable.Id, ExperienceReward = 11 },
            new() { Id = viperId, Name = "Viper", ImagePath = "viper", LootTableId = viperLootTable.Id, ExperienceReward = 12 },
        };

        var forgottenRuinsCreatures = new List<Creature>
        {
            new() { Id = feralGhoulId, Name = "Feral Ghoul", ImagePath = "feral_ghoul", LootTableId = feralGhoulLootTable.Id, ExperienceReward = 15 },
            new() { Id = plagueGhoulId, Name = "Plague Ghoul", ImagePath = "plague_ghoul", LootTableId = plagueGhoulLootTable.Id, ExperienceReward = 16 },
            new() { Id = ravenousGhoulId, Name = "Ravenous Ghoul", ImagePath = "ravenous_ghoul", LootTableId = ravenousGhoulLootTable.Id, ExperienceReward = 17 },
            new() { Id = skeletonArcherId, Name = "Skeleton Archer", ImagePath = "skeleton_archer", LootTableId = skeletonArcherLootTable.Id, ExperienceReward = 18 },
            new() { Id = skeletonMageId, Name = "Skeleton Mage", ImagePath = "skeleton_mage", LootTableId = skeletonMageLootTable.Id, ExperienceReward = 19 },
            new() { Id = skeletonWarriorId, Name = "Skeleton Warrior", ImagePath = "skeleton_warrior", LootTableId = skeletonWarriorLootTable.Id, ExperienceReward = 20 },
        };

        var futureRegionOneCreatures = new List<Creature>
        {
            new() { Id = antWorkerId, Name = "Ant Worker", ImagePath = "ant_worker", LootTableId = antWorkerLootTable.Id, ExperienceReward = 8, Archetype = CreatureArchetype.Support },
            new() { Id = fireAntId, Name = "Fire Ant", ImagePath = "fire_ant", LootTableId = fireAntLootTable.Id, ExperienceReward = 9, Archetype = CreatureArchetype.DPS },
            new() { Id = forestSpiritId, Name = "Forest Spirit", ImagePath = "forest_spirit", LootTableId = forestSpiritLootTable.Id, ExperienceReward = 18, Archetype = CreatureArchetype.Support },
            new() { Id = woodNymphId, Name = "Wood Nymph", ImagePath = "wood_nymph", LootTableId = woodNymphLootTable.Id, ExperienceReward = 18, Archetype = CreatureArchetype.Support },
            new() { Id = giantSpiderId, Name = "Giant Spider", ImagePath = "giant_spider", LootTableId = giantSpiderLootTable.Id, ExperienceReward = 18, Archetype = CreatureArchetype.Bruiser },
            new() { Id = venomousSpiderlingId, Name = "Venomous Spiderling", ImagePath = "venomous_spiderling", LootTableId = venomousSpiderlingLootTable.Id, ExperienceReward = 16, Archetype = CreatureArchetype.Balanced },
        };

        await context.Creatures.AddRangeAsync(lumoRuinsCreatures);
        await context.Creatures.AddRangeAsync(bloodGroveCreatures);
        await context.Creatures.AddRangeAsync(crystalCreekCreatures);
        await context.Creatures.AddRangeAsync(twilightClearingCreatures);
        await context.Creatures.AddRangeAsync(goblinMinesDungeonCreatures);
        await context.Creatures.AddRangeAsync(oakThicketCreatures);
        await context.Creatures.AddRangeAsync(forgottenRuinsCreatures);
        await context.Creatures.AddRangeAsync(futureRegionOneCreatures);


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
            var bloodwoodTable = BuildGatheringLootTable(
                ("bloodwood", 45, 1, 3, false),
                ("living_bark", 15, 1, 1, false));

            await context.LootTables.AddRangeAsync(FlattenLootTables(bloodwoodTable));

            var areas = new List<Area>()
            {
                new Area
                {
                    Id = lumoRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Lumo Ruins",
                    LevelRequirement = 1,
                    Creatures = lumoRuinsAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.969f,
                        0.03f,
                        0.001f,
                    ],
                    DifficultyTier = 1,
                },
                new Area
                {
                    Id = bloodGroveAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Blood Grove",
                    LevelRequirement = 5,
                    Creatures = bloodGroveAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 2,
                    GatheringNodes =
                    [
                        new AreaGatheringNode
                        {
                            Id = "blood_grove_bloodwood_tree",
                            Name = "Bloodwood Tree",
                            AreaId = bloodGroveAreaId,
                            Type = GatheringType.Woodcutting,
                            ProcChance = 0.40f,
                            LootTable = bloodwoodTable
                        }
                    ]
                },
                new Area
                {
                    Id = crystalCreekAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Crystal Creek",
                    LevelRequirement = 10,
                    Creatures = crystalCreekAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 3,
                },
                new Area
                {
                    Id = twilightClearingAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Twilight Clearing",
                    LevelRequirement = 15,
                    Creatures = twilightClearingAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 4,
                },
                new Area
                {
                    Id = oakThicketAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Oak Thicket",
                    LevelRequirement = 20,
                    Creatures = oakThicketAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 5,
                },
                new Area
                {
                    Id = forgottenRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Forgotten Ruins",
                    LevelRequirement = 25,
                    Creatures = forgottenRuinsAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 6,
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

    private static LootTable BuildGatheringLootTable(
        params (string ItemId, int Weight, int MinQuantity, int MaxQuantity, bool IsRare)[] entries)
    {
        var itemTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Weight = entries.Sum(entry => Math.Max(0, entry.Weight)),
            Entries = entries
                .Select(entry => new LootTableItem
                {
                    Id = Guid.NewGuid(),
                    ItemId = entry.ItemId,
                    Weight = entry.Weight,
                    MinQuantity = entry.MinQuantity,
                    MaxQuantity = entry.MaxQuantity,
                    IsRare = entry.IsRare
                })
                .ToList<LootTableEntry>()
        };

        return new LootTable
        {
            Id = Guid.NewGuid(),
            Entries = [itemTable]
        };
    }

    private static IEnumerable<LootTable> FlattenLootTables(LootTable table)
    {
        yield return table;

        foreach (var child in table.Entries.OfType<LootTable>())
        {
            foreach (var nested in FlattenLootTables(child))
            {
                yield return nested;
            }
        }
    }
}
