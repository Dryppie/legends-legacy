using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Profiles;
using Services.LL.Entities.Creatures;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;
using Services.LL.Regions;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerProductionCalibrationTests
{
    [Fact]
    public async Task Floors_1_to_15_use_production_preparation_and_meet_calibration_bands()
    {
        var contentRoot = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            contentRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!, null!, null!, essenceDefinitions, creatureEssences,
            null!, null!, null!, null!, null!, null!);
        var balance = Options.Create(new CraftingBalanceOptions());
        var crafting = new JsonCraftingDefinitionProvider(configuration, contentRoot, jsonOptions);
        var canonical = new CanonicalEquipmentBuildFactory(
            crafting,
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
        var dbOptions = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new LLDbContext(dbOptions);
        db.ItemBases.AddRange(crafting.GetEquipmentBases().Values);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        IRegionCreatureScalingProvider regionScaling = new RegionCreatureScalingProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var creatureAbilities = new JsonCreatureAbilityDefinitionProvider(
            configuration,
            contentRoot,
            jsonOptions);
        var setup = new CombatSetupService(
            new CreatureScaler(regionScaling),
            essenceResolver,
            essenceDefinitions,
            creatureEssences,
            creatureAbilities,
            crafting);
        var definitions = new JsonWorldTowerDefinitionProvider(
            Path.Combine(contentRoot, "Data", "world-tower", "tower-floors.json"),
            jsonOptions);
        var abilityCatalog = new JsonAbilityCatalogProvider(configuration, contentRoot, jsonOptions);
        var runner = new WorldTowerProductionCalibrationRunner(
            definitions,
            new CalibrationGuardianEntityService(definitions),
            new WorldTowerCombatRuntimeFactory(
                new CombatPreparationPipeline(new SnapshotCombatantBuilder(db, setup), setup)),
            new CombatEngineExecutor(
                abilityCatalog,
                essenceDefinitions,
                crafting),
            canonical,
            essenceDefinitions,
            abilityCatalog);

        var report = await runner.RunAsync(new WorldTowerProductionCalibrationOptions(
            SampleCount: 100,
            SeedManifestId: WorldTowerProfileTargetContract.CertificationSeedManifestId,
            UseSharedCohortSeeds: true));
        var profileRequirements = runner.GetProfileScenarioRequirements(1, 15);

        Assert.Equal(45, report.Results.Count);
        Assert.Equal(
            WorldTowerProfileTargetContract.CertificationSeedManifestId,
            report.SeedManifest?.Id);
        Assert.True(report.SeedManifest?.SharedAcrossCohorts);
        Assert.Equal(64, report.InputFingerprint.Length);
        Assert.Equal(
            Enumerable.Range(1, 15),
            profileRequirements.SelectMany(requirement => requirement.FloorNumbers).Order());
        Assert.Equal(15, profileRequirements.Count);
        Assert.Equal(
            profileRequirements.Count,
            profileRequirements.Select(requirement => requirement.ScenarioId).Distinct().Count());
        Assert.All(profileRequirements, requirement =>
        {
            Assert.Contains(requirement.TeamSize, new[] { 5, 10, 15 });
            var floorNumber = Assert.Single(requirement.FloorNumbers);
            Assert.Equal("Balanced", requirement.AuditEquipmentProfile);
            Assert.Contains($"team-{requirement.TeamSize}", requirement.ScenarioId);
            Assert.Contains($"essences-{requirement.EssencesPerParticipant}", requirement.ScenarioId);
            Assert.EndsWith($".floor-{floorNumber}", requirement.ScenarioId);
        });
        Assert.All(report.Results, result => Assert.True(result.AbilitiesStartOnCooldown));
        Assert.All(report.Results, result => Assert.Equal(
            result.FloorNumber switch
            {
                5 or 8 or 9 or 11 or 12 or 13 or 14 => 10,
                10 or 15 => 15,
                _ => 5
            },
            result.RosterSize));
        Assert.All(report.Results, result =>
        {
            Assert.Equal(result.RosterSize, result.PreparedRoster.Count);
            Assert.NotEmpty(result.PreparedGuardian.FinalAttributes);
            Assert.NotEmpty(result.PreparedGuardian.AbilityIds);
        });
        Assert.All(report.Results.Where(result =>
            result.FloorNumber >= 11
            && result.Cohort == WorldTowerCalibrationCohort.Recommended), result =>
        {
            var requirement = WorldTowerEquipmentRequirementCurve.Get(result.FloorNumber);
            var floor = definitions.GetFloors().Single(candidate =>
                candidate.FloorNumber == result.FloorNumber);
            Assert.InRange(
                result.AveragePowerRating,
                floor.RecommendedPowerRating - 1,
                floor.RecommendedPowerRating + 1);
            Assert.Equal(requirement.EssenceCount, result.EssenceCount);
            Assert.All(result.PreparedRoster, combatant =>
            {
                Assert.Equal(7, combatant.Equipment.Count);
                Assert.Equal(requirement.EssenceCount, combatant.EssenceIds.Count);
                Assert.True(combatant.Level > 1);
                Assert.NotEmpty(combatant.FinalAttributes);
                Assert.NotEmpty(combatant.AbilityIds);
                Assert.All(combatant.Equipment, item =>
                {
                    Assert.Equal(requirement.Tier, item.Tier);
                    Assert.Equal(requirement.Rarity, item.Rarity);
                    Assert.Equal(requirement.Quality, item.Quality);
                    Assert.NotEmpty(item.Modifiers);
                });
            });
        });
        Assert.All(report.Results.Where(result => result.FloorNumber <= 10), result =>
            Assert.Equal(1, result.PreparedRoster.SelectMany(combatant => combatant.Equipment)
                .Select(item => item.Tier).Distinct().Single()));
        Assert.All(report.Results.Where(result =>
            result.FloorNumber <= 10
            && result.Cohort == WorldTowerCalibrationCohort.Recommended), result =>
        {
            var floor = definitions.GetFloors().Single(candidate =>
                candidate.FloorNumber == result.FloorNumber);
            Assert.InRange(
                result.AveragePowerRating,
                floor.RecommendedPowerRating - 3,
                floor.RecommendedPowerRating + 3);
            Assert.Equal(result.FloorNumber <= 3 ? 5 : result.FloorNumber <= 6 ? 6 : 7,
                result.EssenceCount);
            Assert.All(result.PreparedRoster.SelectMany(combatant => combatant.Equipment), item =>
                Assert.Equal(ItemQuality.Standard, item.Quality));
        });
        var earlyCalibrationFailures = report.Results
            .Where(result => result.FloorNumber <= 10)
            .Where(result => result.Cohort switch
            {
                WorldTowerCalibrationCohort.BelowRecommended => result.WinRate > 0.20,
                WorldTowerCalibrationCohort.Recommended => result.WinRate < 0.80,
                WorldTowerCalibrationCohort.Stronger => result.WinRate < 0.80,
                _ => false
            })
            .ToArray();
        Assert.True(earlyCalibrationFailures.Length == 0, string.Join(
            Environment.NewLine,
            earlyCalibrationFailures.Select(result =>
                $"Floor {result.FloorNumber} {result.Cohort} " +
                $"({result.EquipmentRungId}, {result.EssenceCount} Essences, " +
                $"rating {result.AveragePowerRating:F1}) won {result.WinRate:P0}.")));
        var lateCalibrationFailures = report.Results
            .Where(result => result.FloorNumber >= 11)
            .Where(result => result.Cohort switch
            {
                WorldTowerCalibrationCohort.BelowRecommended => result.WinRate > 0.20,
                WorldTowerCalibrationCohort.Recommended => result.WinRate is < 0.40 or > 0.70,
                WorldTowerCalibrationCohort.Stronger => result.WinRate < 0.60,
                _ => false
            })
            .ToArray();
        Assert.True(lateCalibrationFailures.Length == 0, string.Join(
            Environment.NewLine,
            lateCalibrationFailures.Select(result =>
                $"Floor {result.FloorNumber} {result.Cohort} " +
                $"({result.EquipmentRungId}, {result.EssenceCount} Essences, " +
                $"rating {result.AveragePowerRating:F1}) won {result.WinRate:P0}.")));
        Assert.All(report.Results.Where(result =>
            result.FloorNumber <= 10
            && result.Cohort == WorldTowerCalibrationCohort.Stronger), result =>
            Assert.InRange(result.WinRate, 0.80, 1.00));
    }

    private sealed class CalibrationGuardianEntityService(
        Application.Interfaces.Services.LL.WorldTower.IWorldTowerDefinitionProvider definitions)
        : IEntityService
    {
        public Task<List<Entity>> GetEntitiesByIdsForCombatAsync(
            List<Guid> entityIds,
            CancellationToken cancellationToken)
        {
            var creatures = entityIds.Select(id =>
            {
                var floor = definitions.GetFloors().Single(candidate => candidate.GuardianCreatureId == id);
                var (archetype, damageProfile, baseLevel, tier) = floor.FloorNumber switch
                {
                    1 => (CreatureArchetype.Tank, DamageProfile.Hybrid, 30, 1),
                    2 => (CreatureArchetype.Bruiser, DamageProfile.Physical, 32, 1),
                    3 => (CreatureArchetype.Tank, DamageProfile.Magical, 34, 1),
                    4 => (CreatureArchetype.Balanced, DamageProfile.Hybrid, 37, 1),
                    5 => (CreatureArchetype.Tank, DamageProfile.Hybrid, 39, 1),
                    6 => (CreatureArchetype.DPS, DamageProfile.Magical, 41, 1),
                    7 => (CreatureArchetype.Support, DamageProfile.Magical, 43, 1),
                    8 => (CreatureArchetype.Balanced, DamageProfile.Magical, 46, 1),
                    9 => (CreatureArchetype.Balanced, DamageProfile.Hybrid, 48, 1),
                    10 => (CreatureArchetype.Bruiser, DamageProfile.Physical, 50, 1),
                    11 => (CreatureArchetype.Balanced, DamageProfile.Magical, 55, 2),
                    12 => (CreatureArchetype.Bruiser, DamageProfile.Magical, 57, 2),
                    13 => (CreatureArchetype.Balanced, DamageProfile.Magical, 59, 2),
                    14 => (CreatureArchetype.Bruiser, DamageProfile.Physical, 61, 2),
                    15 => (CreatureArchetype.Bruiser, DamageProfile.Hybrid, 63, 2),
                    _ => throw new InvalidOperationException()
                };
                return (Entity)new Creature
                {
                    Id = id,
                    Name = floor.GuardianName,
                    Archetype = archetype,
                    DamageProfile = damageProfile,
                    DefenseProfile = DefenseProfile.Balanced,
                    BaseLevel = baseLevel,
                    Tier = tier
                };
            }).ToList();
            return Task.FromResult(creatures);
        }

        public void UpdateEntities(List<Entity> playerCharacters) =>
            throw new NotSupportedException();
    }
}
