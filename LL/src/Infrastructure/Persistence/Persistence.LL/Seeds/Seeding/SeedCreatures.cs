using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Items;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Tutorials;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Seeds.Seeding;
public static class SeedCreatures
{
    public static async Task SeedCreaturesData(this LLDbContext context)
    {
        await SeedCreaturesForShenicRegionLumoRuins(context);
    }

    private static async Task SeedCreaturesForShenicRegionLumoRuins(LLDbContext context)
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
        var blackjawSpiderId = Guid.Parse("00000000-0000-0000-0000-000000000038");
        var ravenId = Guid.Parse("00000000-0000-0000-0000-000000000039");
        var scarecrowId = Guid.Parse("00000000-0000-0000-0000-000000000040");
        var lostSoulId = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var apparitionId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var specterId = Guid.Parse("00000000-0000-0000-0000-000000000043");
        var zombieId = Guid.Parse("00000000-0000-0000-0000-000000000044");
        var halfZombieId = Guid.Parse("00000000-0000-0000-0000-000000000045");
        var undeadId = Guid.Parse("00000000-0000-0000-0000-000000000046");
        var bloodZombieId = Guid.Parse("00000000-0000-0000-0000-000000000047");
        var giantWormId = Guid.Parse("00000000-0000-0000-0000-000000000048");
        var burrowedHorrorId = Guid.Parse("00000000-0000-0000-0000-000000000049");
        var caveLeechId = Guid.Parse("00000000-0000-0000-0000-000000000050");
        var widowStalkerId = Guid.Parse("00000000-0000-0000-0000-000000000051");
        var stonejawGrubId = Guid.Parse("00000000-0000-0000-0000-000000000052");
        var deepBurrowerId = Guid.Parse("00000000-0000-0000-0000-000000000053");
        var trainingGoblinId = Guid.Parse("00000000-0000-0000-0000-000000000054");
        // Step 2 - Create creatures
        var lumoRuinsCreatures = new List<Creature>
        {
            new() { Id = goblinId, Name = "Goblin", ImagePath = "goblin", ExperienceReward = 4 },
            new() { Id = goblinWarriorId, Name = "Goblin Warrior", ImagePath = "goblin_warrior", ExperienceReward = 6, Archetype = CreatureArchetype.Bruiser },
            new() { Id = goblinArcherId, Name = "Goblin Archer", ImagePath = "goblin_archer", ExperienceReward = 5, Archetype = CreatureArchetype.DPS },
            new() { Id = largeRatId, Name = "Large Rat", ImagePath = "large_rat", ExperienceReward = 5, Archetype = CreatureArchetype.Tank }
        };

        var bloodGroveCreatures = new List<Creature>
        {
            new() { Id = flameImpId, Name = "Flame Imp", ImagePath = "flame_imp", ExperienceReward = 6 },
            new() { Id = frostImpId, Name = "Frost Imp", ImagePath = "frost_imp", ExperienceReward = 6 },
            new() { Id = shadowImpId, Name = "Shadow Imp", ImagePath = "shadow_imp", ExperienceReward = 6 },
            new() { Id = vampireBatId, Name = "Vampire Bat", ImagePath = "vampire_bat", ExperienceReward = 8 }
        };

        var crystalCreekCreatures = new List<Creature>
        {
            new() { Id = blueSlimeId, Name = "Blue Slime", ImagePath = "blue_slime", ExperienceReward = 10 },
            new() { Id = brownSlimeId, Name = "Brown Slime", ImagePath = "brown_slime", ExperienceReward = 10 },
            new() { Id = greenSlimeId, Name = "Green Slime", ImagePath = "green_slime", ExperienceReward = 10 },
            new() { Id = rainbowSlimeId, Name = "Rainbow Slime", ImagePath = "rainbow_slime", ExperienceReward = 12 },
            new() { Id = redSlimeId, Name = "Red Slime", ImagePath = "red_slime", ExperienceReward = 10 },
            new() { Id = transparentSlimeId, Name = "Transparent Slime", ImagePath = "transparent_slime", ExperienceReward = 11 },
        };

        var twilightClearingCreatures = new List<Creature>
        {
            new() { Id = enchantedFairyId, Name = "Enchanted Fairy", ImagePath = "enchanted_fairy", ExperienceReward = 14 },
            new() { Id = gladePantherId, Name = "Glade Panther", ImagePath = "glade_panther", ExperienceReward = 14 },
            new() { Id = illusionFoxId, Name = "Illusion Fox", ImagePath = "illusion_fox", ExperienceReward = 14 },
            new() { Id = nightshadeBlossomId, Name = "Nightshade Blossom", ImagePath = "nightshade_blossom", ExperienceReward = 15 },
            new() { Id = pixieId, Name = "Pixie", ImagePath = "pixie", ExperienceReward = 12 },
        };

        var goblinMinesDungeonCreatures = new List<Creature>
        {
            new() { Id = hobgoblinId, Name = "Hobgoblin", ImagePath = "hobgoblin", ExperienceReward = 20 },
        };

        var oakThicketCreatures = new List<Creature>
        {
            new() { Id = mossLizardId, Name = "Moss Lizard", ImagePath = "moss_lizard", ExperienceReward = 8 },
            new() { Id = spiderId, Name = "Spider", ImagePath = "spider", ExperienceReward = 9 },
            new() { Id = treantSaplingId, Name = "Treant Sapling", ImagePath = "treant_sapling", ExperienceReward = 10 },
            new() { Id = venomousSnakeId, Name = "Venomous Snake", ImagePath = "venomous_snake", ExperienceReward = 11 },
            new() { Id = viperId, Name = "Viper", ImagePath = "viper", ExperienceReward = 12 },
        };

        var forgottenRuinsCreatures = new List<Creature>
        {
            new() { Id = feralGhoulId, Name = "Feral Ghoul", ImagePath = "feral_ghoul", ExperienceReward = 15 },
            new() { Id = plagueGhoulId, Name = "Plague Ghoul", ImagePath = "plague_ghoul", ExperienceReward = 16 },
            new() { Id = ravenousGhoulId, Name = "Ravenous Ghoul", ImagePath = "ravenous_ghoul", ExperienceReward = 17 },
            new() { Id = skeletonArcherId, Name = "Skeleton Archer", ImagePath = "skeleton_archer", ExperienceReward = 18 },
            new() { Id = skeletonMageId, Name = "Skeleton Mage", ImagePath = "skeleton_mage", ExperienceReward = 19 },
            new() { Id = skeletonWarriorId, Name = "Skeleton Warrior", ImagePath = "skeleton_warrior", ExperienceReward = 20 },
        };

        var futureRegionOneCreatures = new List<Creature>
        {
            new() { Id = antWorkerId, Name = "Ant Worker", ImagePath = "ant_worker", ExperienceReward = 8, Archetype = CreatureArchetype.Support },
            new() { Id = fireAntId, Name = "Fire Ant", ImagePath = "fire_ant", ExperienceReward = 9, Archetype = CreatureArchetype.DPS },
            new() { Id = forestSpiritId, Name = "Forest Spirit", ImagePath = "forest_spirit", ExperienceReward = 18, Archetype = CreatureArchetype.Support },
            new() { Id = woodNymphId, Name = "Wood Nymph", ImagePath = "wood_nymph", ExperienceReward = 18, Archetype = CreatureArchetype.Support },
            new() { Id = giantSpiderId, Name = "Giant Spider", ImagePath = "giant_spider", ExperienceReward = 18, Archetype = CreatureArchetype.Bruiser },
            new() { Id = venomousSpiderlingId, Name = "Venomous Spiderling", ImagePath = "venomous_spiderling", ExperienceReward = 16, Archetype = CreatureArchetype.Balanced },
        };

        var remainingRegionOneIdleCreatures = new List<Creature>
        {
            new() { Id = blackjawSpiderId, Name = "Blackjaw Spider", ImagePath = "blackjaw_spider", ExperienceReward = 19, Archetype = CreatureArchetype.Bruiser },
            new() { Id = ravenId, Name = "Raven", ImagePath = "raven", ExperienceReward = 16, Archetype = CreatureArchetype.Balanced },
            new() { Id = widowStalkerId, Name = "Widow Stalker", ImagePath = "widow_stalker", ExperienceReward = 20, Archetype = CreatureArchetype.DPS },
            new() { Id = scarecrowId, Name = "Scarecrow", ImagePath = "scarecrow", ExperienceReward = 19, Archetype = CreatureArchetype.Hazard },
            new() { Id = lostSoulId, Name = "Lost Soul", ImagePath = "lost_soul", ExperienceReward = 18, Archetype = CreatureArchetype.Support },
            new() { Id = apparitionId, Name = "Apparition", ImagePath = "apparition", ExperienceReward = 20, Archetype = CreatureArchetype.Hazard },
            new() { Id = specterId, Name = "Specter", ImagePath = "specter", ExperienceReward = 21, Archetype = CreatureArchetype.DPS },
            new() { Id = zombieId, Name = "Zombie", ImagePath = "zombie", ExperienceReward = 20, Archetype = CreatureArchetype.Tank },
            new() { Id = halfZombieId, Name = "Half Zombie", ImagePath = "half_zombie", ExperienceReward = 19, Archetype = CreatureArchetype.Balanced },
            new() { Id = undeadId, Name = "Undead", ImagePath = "undead", ExperienceReward = 21, Archetype = CreatureArchetype.Bruiser },
            new() { Id = bloodZombieId, Name = "Blood Zombie", ImagePath = "blood_zombie", ExperienceReward = 22, Archetype = CreatureArchetype.Tank },
            new() { Id = giantWormId, Name = "Giant Worm", ImagePath = "giant_worm", ExperienceReward = 23, Archetype = CreatureArchetype.Bruiser },
            new() { Id = burrowedHorrorId, Name = "Burrowed Horror", ImagePath = "burrowed_horror", ExperienceReward = 24, Archetype = CreatureArchetype.DPS },
            new() { Id = caveLeechId, Name = "Cave Leech", ImagePath = "cave_leech", ExperienceReward = 22, Archetype = CreatureArchetype.Support },
            new() { Id = stonejawGrubId, Name = "Stonejaw Grub", ImagePath = "stonejaw_grub", ExperienceReward = 21, Archetype = CreatureArchetype.Tank },
            new() { Id = deepBurrowerId, Name = "Deep Burrower", ImagePath = "deep_burrower", ExperienceReward = 25, Archetype = CreatureArchetype.Bruiser },
        };

        var tutorialCreatures = new List<Creature>
        {
            new()
            {
                Id = trainingGoblinId,
                Name = "Training Goblin",
                ImagePath = "goblin",
                ExperienceReward = 1,
                Archetype = CreatureArchetype.Balanced,
                StatOverrides = BuildTrainingGoblinStatOverrides()
            }
        };

        await context.Creatures.AddRangeAsync(lumoRuinsCreatures);
        await context.Creatures.AddRangeAsync(bloodGroveCreatures);
        await context.Creatures.AddRangeAsync(crystalCreekCreatures);
        await context.Creatures.AddRangeAsync(twilightClearingCreatures);
        await context.Creatures.AddRangeAsync(goblinMinesDungeonCreatures);
        await context.Creatures.AddRangeAsync(oakThicketCreatures);
        await context.Creatures.AddRangeAsync(forgottenRuinsCreatures);
        await context.Creatures.AddRangeAsync(futureRegionOneCreatures);
        await context.Creatures.AddRangeAsync(remainingRegionOneIdleCreatures);
        await context.Creatures.AddRangeAsync(tutorialCreatures);


        // Step 6 - Create area
        var lumoRuinsAreaId = "region_01_area_01";
        var trainingGroundsAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = TutorialConstants.TrainingGroundsAreaId, CreatureId = trainingGoblinId, WeightedSpawnRate = 1f },
        };

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
        var oldForestAreaId = "region_01_area_08";
        var bleakOrchardAreaId = "region_01_area_09";
        var rottingHamletAreaId = "region_01_area_10";
        var wormburrowDepthsAreaId = "region_01_area_11";

        var oldForestAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = oldForestAreaId, CreatureId = giantSpiderId, WeightedSpawnRate = 0.22f },
            new AreaCreature() { AreaId = oldForestAreaId, CreatureId = venomousSpiderlingId, WeightedSpawnRate = 0.28f },
            new AreaCreature() { AreaId = oldForestAreaId, CreatureId = blackjawSpiderId, WeightedSpawnRate = 0.22f },
            new AreaCreature() { AreaId = oldForestAreaId, CreatureId = ravenId, WeightedSpawnRate = 0.13f },
            new AreaCreature() { AreaId = oldForestAreaId, CreatureId = widowStalkerId, WeightedSpawnRate = 0.15f },
        };

        var bleakOrchardAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = bleakOrchardAreaId, CreatureId = scarecrowId, WeightedSpawnRate = 0.30f },
            new AreaCreature() { AreaId = bleakOrchardAreaId, CreatureId = lostSoulId, WeightedSpawnRate = 0.30f },
            new AreaCreature() { AreaId = bleakOrchardAreaId, CreatureId = apparitionId, WeightedSpawnRate = 0.20f },
            new AreaCreature() { AreaId = bleakOrchardAreaId, CreatureId = specterId, WeightedSpawnRate = 0.20f },
        };

        var rottingHamletAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = rottingHamletAreaId, CreatureId = zombieId, WeightedSpawnRate = 0.35f },
            new AreaCreature() { AreaId = rottingHamletAreaId, CreatureId = halfZombieId, WeightedSpawnRate = 0.25f },
            new AreaCreature() { AreaId = rottingHamletAreaId, CreatureId = undeadId, WeightedSpawnRate = 0.25f },
            new AreaCreature() { AreaId = rottingHamletAreaId, CreatureId = bloodZombieId, WeightedSpawnRate = 0.15f },
        };

        var wormburrowDepthsAreaCreatures = new List<AreaCreature>
        {
            new AreaCreature() { AreaId = wormburrowDepthsAreaId, CreatureId = giantWormId, WeightedSpawnRate = 0.30f },
            new AreaCreature() { AreaId = wormburrowDepthsAreaId, CreatureId = burrowedHorrorId, WeightedSpawnRate = 0.22f },
            new AreaCreature() { AreaId = wormburrowDepthsAreaId, CreatureId = caveLeechId, WeightedSpawnRate = 0.18f },
            new AreaCreature() { AreaId = wormburrowDepthsAreaId, CreatureId = stonejawGrubId, WeightedSpawnRate = 0.16f },
            new AreaCreature() { AreaId = wormburrowDepthsAreaId, CreatureId = deepBurrowerId, WeightedSpawnRate = 0.14f },
        };

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
                    Id = TutorialConstants.TrainingGroundsAreaId,
                    Name = "Training Area",
                    LevelRequirement = 1,
                    Creatures = trainingGroundsAreaCreatures,
                    SpawnProbabilities =
                    [
                        1f
                    ],
                    DifficultyTier = 0,
                },
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
                            RewardTableId = "reward.gathering.blood_grove.bloodwood_tree"
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
                    Id = oldForestAreaId,
                    Name = "Old Forest",
                    LevelRequirement = 25,
                    Creatures = oldForestAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 6,
                },
                new Area
                {
                    Id = bleakOrchardAreaId,
                    Name = "Bleak Orchard",
                    LevelRequirement = 30,
                    Creatures = bleakOrchardAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 7,
                },
                new Area
                {
                    Id = rottingHamletAreaId,
                    Name = "Rotting Hamlet",
                    LevelRequirement = 35,
                    Creatures = rottingHamletAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 8,
                },
                new Area
                {
                    Id = wormburrowDepthsAreaId,
                    Name = "Wormburrow Depths",
                    LevelRequirement = 40,
                    Creatures = wormburrowDepthsAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 9,
                },
                new Area
                {
                    Id = forgottenRuinsAreaId, // region, [area, dungeon, raid, or rift], area
                    Name = "Forgotten Ruins",
                    LevelRequirement = 45,
                    Creatures = forgottenRuinsAreaCreatures,
                    SpawnProbabilities =
                    [
                        0.03f,
                        0.969f,
                        0.001f,
                    ],
                    DifficultyTier = 10,
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

    public static async Task<bool> EnsureRemainingRegionOneIdleAreas(LLDbContext context)
    {
        var shenic = await context.Regions
            .Include(region => region.Areas)
            .FirstOrDefaultAsync(region => region.Name == "Shenic");

        if (shenic is null)
        {
            return false;
        }

        var changed = false;
        changed |= await EnsureTutorialTrainingGroundsAsync(context, shenic);

        var creatureSeeds = BuildRemainingRegionOneIdleCreatureSeeds();
        var creatureIds = creatureSeeds.Select(creature => creature.Id).ToArray();
        var existingCreatureIds = await context.Creatures
            .Where(creature => creatureIds.Contains(creature.Id))
            .Select(creature => creature.Id)
            .ToListAsync();
        var existingCreatureIdSet = existingCreatureIds.ToHashSet();

        foreach (var seed in creatureSeeds.Where(seed => !existingCreatureIdSet.Contains(seed.Id)))
        {
            context.Creatures.Add(new Creature
            {
                Id = seed.Id,
                Name = seed.Name,
                ImagePath = seed.ImagePath,
                ExperienceReward = seed.ExperienceReward,
                Archetype = seed.Archetype
            });
            changed = true;
        }

        foreach (var areaSeed in BuildRemainingRegionOneIdleAreaSeeds())
        {
            var area = await context.Areas
                .Include(existingArea => existingArea.Creatures)
                .FirstOrDefaultAsync(existingArea => existingArea.Id == areaSeed.Id);

            if (area is null)
            {
                area = new Area
                {
                    Id = areaSeed.Id,
                    Name = areaSeed.Name,
                    LevelRequirement = areaSeed.LevelRequirement,
                    DifficultyTier = areaSeed.DifficultyTier,
                    SpawnProbabilities = [0.03f, 0.969f, 0.001f],
                    Creatures = areaSeed.Creatures
                        .Select(creature => new AreaCreature
                        {
                            AreaId = areaSeed.Id,
                            CreatureId = creature.CreatureId,
                            WeightedSpawnRate = creature.WeightedSpawnRate
                        })
                        .ToList()
                };

                shenic.Areas.Add(area);
                changed = true;
                continue;
            }

            var expectedSpawnProbabilities = new List<float> { 0.03f, 0.969f, 0.001f };
            if (area.Name != areaSeed.Name
                || area.LevelRequirement != areaSeed.LevelRequirement
                || area.DifficultyTier != areaSeed.DifficultyTier
                || !area.SpawnProbabilities.SequenceEqual(expectedSpawnProbabilities))
            {
                changed = true;
            }

            area.Name = areaSeed.Name;
            area.LevelRequirement = areaSeed.LevelRequirement;
            area.DifficultyTier = areaSeed.DifficultyTier;
            area.SpawnProbabilities = expectedSpawnProbabilities;

            foreach (var creatureSeed in areaSeed.Creatures)
            {
                var existingAreaCreature = area.Creatures.FirstOrDefault(creature => creature.CreatureId == creatureSeed.CreatureId);
                if (existingAreaCreature is not null)
                {
                    if (Math.Abs(existingAreaCreature.WeightedSpawnRate - creatureSeed.WeightedSpawnRate) > 0.0001f)
                    {
                        existingAreaCreature.WeightedSpawnRate = creatureSeed.WeightedSpawnRate;
                        changed = true;
                    }

                    continue;
                }

                area.Creatures.Add(new AreaCreature
                {
                    AreaId = areaSeed.Id,
                    CreatureId = creatureSeed.CreatureId,
                    WeightedSpawnRate = creatureSeed.WeightedSpawnRate
                });
                changed = true;
            }
        }

        var forgottenRuins = await context.Areas
            .FirstOrDefaultAsync(area => area.Id == "region_01_area_07");
        if (forgottenRuins is not null
            && (forgottenRuins.LevelRequirement != 45 || forgottenRuins.DifficultyTier != 10))
        {
            forgottenRuins.LevelRequirement = 45;
            forgottenRuins.DifficultyTier = 10;
            changed = true;
        }

        return changed;
    }

    private static async Task<bool> EnsureTutorialTrainingGroundsAsync(LLDbContext context, Region shenic)
    {
        var changed = false;
        var trainingGoblinId = Guid.Parse("00000000-0000-0000-0000-000000000054");
        var trainingGoblin = await context.Creatures
            .Include(creature => creature.StatOverrides)
            .FirstOrDefaultAsync(creature => creature.Id == trainingGoblinId);

        if (trainingGoblin is null)
        {
            context.Creatures.Add(new Creature
            {
                Id = trainingGoblinId,
                Name = "Training Goblin",
                ImagePath = "goblin",
                ExperienceReward = 1,
                Archetype = CreatureArchetype.Balanced,
                StatOverrides = BuildTrainingGoblinStatOverrides()
            });
            changed = true;
        }
        else if (SynchronizeTrainingGoblinStatOverrides(trainingGoblin))
        {
            changed = true;
        }

        var trainingGrounds = await context.Areas
            .Include(area => area.Creatures)
            .FirstOrDefaultAsync(area => area.Id == TutorialConstants.TrainingGroundsAreaId);

        if (trainingGrounds is null)
        {
            shenic.Areas.Add(new Area
            {
                Id = TutorialConstants.TrainingGroundsAreaId,
                Name = "Training Area",
                LevelRequirement = 1,
                DifficultyTier = 0,
                SpawnProbabilities = [1f],
                Creatures =
                [
                    new AreaCreature
                    {
                        AreaId = TutorialConstants.TrainingGroundsAreaId,
                        CreatureId = trainingGoblinId,
                        WeightedSpawnRate = 1f
                    }
                ]
            });
            return true;
        }

        if (trainingGrounds.Name != "Training Area"
            || trainingGrounds.LevelRequirement != 1
            || trainingGrounds.DifficultyTier != 0
            || !trainingGrounds.SpawnProbabilities.SequenceEqual(new List<float> { 1f }))
        {
            trainingGrounds.Name = "Training Area";
            trainingGrounds.LevelRequirement = 1;
            trainingGrounds.DifficultyTier = 0;
            trainingGrounds.SpawnProbabilities = [1f];
            changed = true;
        }

        var areaCreature = trainingGrounds.Creatures.FirstOrDefault(creature => creature.CreatureId == trainingGoblinId);
        if (areaCreature is null)
        {
            trainingGrounds.Creatures.Add(new AreaCreature
            {
                AreaId = TutorialConstants.TrainingGroundsAreaId,
                CreatureId = trainingGoblinId,
                WeightedSpawnRate = 1f
            });
            changed = true;
        }
        else if (Math.Abs(areaCreature.WeightedSpawnRate - 1f) > 0.0001f)
        {
            areaCreature.WeightedSpawnRate = 1f;
            changed = true;
        }

        return changed;
    }

    private static IReadOnlyList<RegionOneCreatureSeed> BuildRemainingRegionOneIdleCreatureSeeds() =>
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000036"), "Giant Spider", "giant_spider", 18, CreatureArchetype.Bruiser),
        new(Guid.Parse("00000000-0000-0000-0000-000000000037"), "Venomous Spiderling", "venomous_spiderling", 16, CreatureArchetype.Balanced),
        new(Guid.Parse("00000000-0000-0000-0000-000000000038"), "Blackjaw Spider", "blackjaw_spider", 19, CreatureArchetype.Bruiser),
        new(Guid.Parse("00000000-0000-0000-0000-000000000039"), "Raven", "raven", 16, CreatureArchetype.Balanced),
        new(Guid.Parse("00000000-0000-0000-0000-000000000051"), "Widow Stalker", "widow_stalker", 20, CreatureArchetype.DPS),
        new(Guid.Parse("00000000-0000-0000-0000-000000000040"), "Scarecrow", "scarecrow", 19, CreatureArchetype.Hazard),
        new(Guid.Parse("00000000-0000-0000-0000-000000000041"), "Lost Soul", "lost_soul", 18, CreatureArchetype.Support),
        new(Guid.Parse("00000000-0000-0000-0000-000000000042"), "Apparition", "apparition", 20, CreatureArchetype.Hazard),
        new(Guid.Parse("00000000-0000-0000-0000-000000000043"), "Specter", "specter", 21, CreatureArchetype.DPS),
        new(Guid.Parse("00000000-0000-0000-0000-000000000044"), "Zombie", "zombie", 20, CreatureArchetype.Tank),
        new(Guid.Parse("00000000-0000-0000-0000-000000000045"), "Half Zombie", "half_zombie", 19, CreatureArchetype.Balanced),
        new(Guid.Parse("00000000-0000-0000-0000-000000000046"), "Undead", "undead", 21, CreatureArchetype.Bruiser),
        new(Guid.Parse("00000000-0000-0000-0000-000000000047"), "Blood Zombie", "blood_zombie", 22, CreatureArchetype.Tank),
        new(Guid.Parse("00000000-0000-0000-0000-000000000048"), "Giant Worm", "giant_worm", 23, CreatureArchetype.Bruiser),
        new(Guid.Parse("00000000-0000-0000-0000-000000000049"), "Burrowed Horror", "burrowed_horror", 24, CreatureArchetype.DPS),
        new(Guid.Parse("00000000-0000-0000-0000-000000000050"), "Cave Leech", "cave_leech", 22, CreatureArchetype.Support),
        new(Guid.Parse("00000000-0000-0000-0000-000000000052"), "Stonejaw Grub", "stonejaw_grub", 21, CreatureArchetype.Tank),
        new(Guid.Parse("00000000-0000-0000-0000-000000000053"), "Deep Burrower", "deep_burrower", 25, CreatureArchetype.Bruiser),
    ];

    private static IReadOnlyList<RegionOneAreaSeed> BuildRemainingRegionOneIdleAreaSeeds() =>
    [
        new(
            "region_01_area_08",
            "Old Forest",
            25,
            6,
            [
                new(Guid.Parse("00000000-0000-0000-0000-000000000036"), 0.22f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000037"), 0.28f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000038"), 0.22f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000039"), 0.13f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000051"), 0.15f),
            ]),
        new(
            "region_01_area_09",
            "Bleak Orchard",
            30,
            7,
            [
                new(Guid.Parse("00000000-0000-0000-0000-000000000040"), 0.30f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000041"), 0.30f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000042"), 0.20f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000043"), 0.20f),
            ]),
        new(
            "region_01_area_10",
            "Rotting Hamlet",
            35,
            8,
            [
                new(Guid.Parse("00000000-0000-0000-0000-000000000044"), 0.35f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000045"), 0.25f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000046"), 0.25f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000047"), 0.15f),
            ]),
        new(
            "region_01_area_11",
            "Wormburrow Depths",
            40,
            9,
            [
                new(Guid.Parse("00000000-0000-0000-0000-000000000048"), 0.30f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000049"), 0.22f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000050"), 0.18f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000052"), 0.16f),
                new(Guid.Parse("00000000-0000-0000-0000-000000000053"), 0.14f),
            ]),
    ];

    private static List<StatOverride> BuildTrainingGoblinStatOverrides() =>
    [
        new() { AttributeType = AttributeType.MaxHealth, Multiplier = 0.20f },
        new() { AttributeType = AttributeType.Power, Multiplier = 0.10f },
        new() { AttributeType = AttributeType.WeaponDamage, Multiplier = 0.10f },
        new() { AttributeType = AttributeType.Precision, Multiplier = 0.50f }
    ];

    private static bool SynchronizeTrainingGoblinStatOverrides(Creature trainingGoblin)
    {
        var changed = false;
        var expected = BuildTrainingGoblinStatOverrides();

        foreach (var expectedOverride in expected)
        {
            var existing = trainingGoblin.StatOverrides
                .FirstOrDefault(statOverride => statOverride.AttributeType == expectedOverride.AttributeType);

            if (existing is null)
            {
                trainingGoblin.StatOverrides.Add(expectedOverride);
                changed = true;
                continue;
            }

            if (existing.Multiplier != expectedOverride.Multiplier || existing.Additive != expectedOverride.Additive)
            {
                existing.Multiplier = expectedOverride.Multiplier;
                existing.Additive = expectedOverride.Additive;
                changed = true;
            }
        }

        return changed;
    }

    private sealed record RegionOneCreatureSeed(
        Guid Id,
        string Name,
        string ImagePath,
        int ExperienceReward,
        CreatureArchetype Archetype);

    private sealed record RegionOneAreaSeed(
        string Id,
        string Name,
        int LevelRequirement,
        int DifficultyTier,
        IReadOnlyList<RegionOneAreaCreatureSeed> Creatures);

    private sealed record RegionOneAreaCreatureSeed(Guid CreatureId, float WeightedSpawnRate);
}
