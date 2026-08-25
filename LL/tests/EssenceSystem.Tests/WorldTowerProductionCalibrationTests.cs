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
    public async Task Floors_11_to_15_use_production_preparation_for_all_three_cohorts()
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
        var runner = new WorldTowerProductionCalibrationRunner(
            definitions,
            new CalibrationGuardianEntityService(definitions),
            new WorldTowerCombatRuntimeFactory(new SnapshotCombatantBuilder(db, setup), setup),
            new CombatEngineExecutor(
                new JsonAbilityCatalogProvider(configuration, contentRoot, jsonOptions),
                essenceDefinitions,
                crafting),
            canonical,
            essenceDefinitions);

        var report = await runner.RunAsync(new WorldTowerProductionCalibrationOptions(
            MinimumFloor: 11,
            MaximumFloor: 15,
            SampleCount: 10));

        Assert.Equal(15, report.Results.Count);
        Assert.All(report.Results, result => Assert.True(result.AbilitiesStartOnCooldown));
        Assert.All(report.Results.Where(result => result.FloorNumber < 15), result =>
            Assert.Equal(10, result.RosterSize));
        Assert.All(report.Results.Where(result => result.FloorNumber == 15), result =>
            Assert.Equal(15, result.RosterSize));
        Assert.All(report.Results, result =>
        {
            Assert.Equal(result.RosterSize, result.PreparedRoster.Count);
            Assert.NotEmpty(result.PreparedGuardian.FinalAttributes);
            Assert.NotEmpty(result.PreparedGuardian.AbilityIds);
        });
        Assert.All(report.Results.Where(result =>
            result.Cohort == WorldTowerCalibrationCohort.Tier2EpicExceptional), result =>
        {
            Assert.Equal("t2-exceptional-epic", result.EquipmentRungId);
            Assert.All(result.PreparedRoster, combatant =>
            {
                Assert.Equal(7, combatant.Equipment.Count);
                Assert.Equal(7, combatant.EssenceIds.Count);
                Assert.True(combatant.Level > 1);
                Assert.NotEmpty(combatant.FinalAttributes);
                Assert.NotEmpty(combatant.AbilityIds);
                Assert.All(combatant.Equipment, item =>
                {
                    Assert.Equal(2, item.Tier);
                    Assert.Equal(Rarity.Epic, item.Rarity);
                    Assert.Equal(ItemQuality.Exceptional, item.Quality);
                    Assert.NotEmpty(item.Modifiers);
                });
            });
        });
        Assert.All(report.Results.Where(result =>
            result.Cohort == WorldTowerCalibrationCohort.GearScore220), result =>
            Assert.InRange(result.WinRate, 0, 0.20));
        Assert.InRange(report.Results.Single(result =>
            result.FloorNumber == 11
            && result.Cohort == WorldTowerCalibrationCohort.Tier2EpicExceptional).WinRate,
            0.80,
            1.00);
        Assert.All(report.Results.Where(result =>
            result.FloorNumber >= 12
            && result.Cohort == WorldTowerCalibrationCohort.Tier2EpicExceptional), result =>
            Assert.InRange(result.WinRate, 0, 0.20));
        Assert.All(report.Results.Where(result =>
            result.Cohort == WorldTowerCalibrationCohort.Stronger), result =>
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
                var (archetype, damageProfile) = floor.FloorNumber switch
                {
                    11 => (CreatureArchetype.Balanced, DamageProfile.Magical),
                    12 => (CreatureArchetype.Bruiser, DamageProfile.Magical),
                    13 => (CreatureArchetype.Balanced, DamageProfile.Magical),
                    14 => (CreatureArchetype.Bruiser, DamageProfile.Physical),
                    15 => (CreatureArchetype.Bruiser, DamageProfile.Hybrid),
                    _ => throw new InvalidOperationException()
                };
                return (Entity)new Creature
                {
                    Id = id,
                    Name = floor.GuardianName,
                    Archetype = archetype,
                    DamageProfile = damageProfile,
                    DefenseProfile = DefenseProfile.Balanced,
                    BaseLevel = 33 + floor.ProgressionPosition * 2,
                    Tier = 2
                };
            }).ToList();
            return Task.FromResult(creatures);
        }

        public void UpdateEntities(List<Entity> playerCharacters) =>
            throw new NotSupportedException();
    }
}
