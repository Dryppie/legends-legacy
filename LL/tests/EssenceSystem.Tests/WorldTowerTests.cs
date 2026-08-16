using System.Text.Json;
using System.Text.Json.Serialization;
using Application.MediatR.Attributes;
using Application.UseCases.WorldTower;
using Domain.Models.Combat.Abilities;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerTests
{
    [Fact]
    public void MultiPhaseStartCommandOwnsItsTransactionBoundaries()
    {
        Assert.True(Attribute.IsDefined(
            typeof(StartTowerRallyCommand),
            typeof(NonTransactionalAttribute)));
    }

    [Fact]
    public void CatalogReleasesTenContiguousFloorsUsingExistingCreatures()
    {
        var apiRoot = Environment.GetEnvironmentVariable("LL_TEST_API_ROOT")
            ?? TestContentPaths.FindApiRoot();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var provider = new JsonWorldTowerDefinitionProvider(
            Path.Combine(apiRoot, "Data", "world-tower", "tower-floors.json"),
            options);

        using var creatureDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(apiRoot, "Data", "world", "creatures.json")));
        var creatureIds = creatureDocument.RootElement
            .GetProperty("creatures")
            .EnumerateArray()
            .Select(x => x.GetProperty("id").GetGuid())
            .ToHashSet();
        var floors = provider.GetFloors();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var creatureAbilities = new JsonCreatureAbilityDefinitionProvider(
            configuration,
            apiRoot,
            options);
        var abilityCatalog = new JsonAbilityCatalogProvider(
            configuration,
            apiRoot,
            options).GetCatalog();

        Assert.Equal(Enumerable.Range(1, 10), floors.Select(x => x.FloorNumber));
        Assert.Equal([5, 5, 5, 5, 10, 5, 3, 10, 10, 15], floors.Select(x => x.RequiredSlots));
        Assert.Null(provider.GetFloor(11));
        Assert.All(floors, floor => Assert.Contains(floor.GuardianCreatureId, creatureIds));
        Assert.All(floors, floor => Assert.False(string.IsNullOrWhiteSpace(floor.GuardianAbilityProfileId)));
        Assert.All(floors, floor =>
        {
            var abilityIds = creatureAbilities.GetAbilityIds(floor.GuardianAbilityProfileId);
            Assert.InRange(abilityIds.Count, 1, 4);
            var abilities = abilityIds
                .Select(abilityId => abilityCatalog.AbilitiesById[abilityId])
                .OrderBy(ability => ability.Kind == AbilitySpecKind.Passive ? 1 : 0)
                .ToArray();
            Assert.InRange(abilities.Count(ability => ability.Kind == AbilitySpecKind.Passive), 0, 1);
            var passive = abilities.SingleOrDefault(ability => ability.Kind == AbilitySpecKind.Passive);
            if (passive is not null)
                Assert.Same(passive, abilities[^1]);
        });
        Assert.All(floors, floor => Assert.True(floor.GuardianScaling.Health > 0));
        Assert.All(floors, floor => Assert.True(floor.RecommendedPowerRating >= 0));
        Assert.Equal([100, 104, 107, 109, 112, 114, 116, 118, 120, 122], floors.Select(x => x.TowerTokens));
        Assert.All(floors, floor => Assert.Equal(floor.TowerTokens * 4, floor.FirstClearTowerTokens));
        Assert.True(floors.Zip(floors.Skip(1), (current, next) => next.TowerTokens > current.TowerTokens).All(x => x));
        Assert.All(floors, floor => Assert.Equal(1, floor.BalanceBenchmark.EquipmentTier));
        Assert.Equal(30, floors[0].BalanceBenchmark.CharacterLevel);
        Assert.Equal(Domain.Models.Items.Rarity.Rare, floors[0].BalanceBenchmark.EquipmentRarity);
        Assert.Equal(4, floors[0].BalanceBenchmark.EssenceCount);
        Assert.Equal(5, floors[0].RequiredSlots);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000056"), floors[0].GuardianCreatureId);
        Assert.Equal("Garran, the Gatekeeper", floors[0].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000057"), floors[1].GuardianCreatureId);
        Assert.Equal("Velka, the Bloodwing Huntress", floors[1].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000059"), floors[2].GuardianCreatureId);
        Assert.Equal("Morrowmaw, Broodkeeper", floors[2].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000058"), floors[3].GuardianCreatureId);
        Assert.Equal("Vaelor, the Mirrorbound", floors[3].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000060"), floors[4].GuardianCreatureId);
        Assert.Equal("Kharad, the First Warden", floors[4].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000061"), floors[5].GuardianCreatureId);
        Assert.Equal("Orsenn, the Ashen Bellkeeper", floors[5].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000064"), floors[6].GuardianCreatureId);
        Assert.Equal("Eydis, the Endless Spring", floors[6].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000063"), floors[7].GuardianCreatureId);
        Assert.Equal("Kodoku, the Poisoned Vessel", floors[7].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000062"), floors[8].GuardianCreatureId);
        Assert.Equal("Ni, the Ninefold", floors[8].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000065"), floors[9].GuardianCreatureId);
        Assert.Equal("The Mad King", floors[9].GuardianName);
        Assert.Equal("monster.the_mad_king", floors[9].GuardianAbilityProfileId);
        Assert.Equal(50, floors[^1].BalanceBenchmark.CharacterLevel);
        Assert.Equal(Domain.Models.Items.Rarity.Legendary, floors[^1].BalanceBenchmark.EquipmentRarity);
        Assert.Equal(6, floors[^1].BalanceBenchmark.EssenceCount);
        Assert.Contains(
            floors.Single(x => x.FloorNumber == 1).Unlocks,
            unlock => unlock.Key == "tower_echo_mode_unlock"
                      && unlock.Description.Contains("Echo Mode", StringComparison.Ordinal));
    }

    [Fact]
    public void AuthoredTierOneBandProgressesFromRareToLegendary()
    {
        var apiRoot = Environment.GetEnvironmentVariable("LL_TEST_API_ROOT")
            ?? TestContentPaths.FindApiRoot();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var catalog = JsonSerializer.Deserialize<WorldTowerCatalogDocument>(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "world-tower", "tower-floors.json")),
            options);

        Assert.NotNull(catalog);
        var floors = catalog.Floors.OrderBy(floor => floor.FloorNumber).ToArray();
        Assert.Equal(Enumerable.Range(1, 10), floors.Select(floor => floor.FloorNumber));
        Assert.Equal(
            [
                Domain.Models.Items.Rarity.Rare,
                Domain.Models.Items.Rarity.Rare,
                Domain.Models.Items.Rarity.Rare,
                Domain.Models.Items.Rarity.Epic,
                Domain.Models.Items.Rarity.Epic,
                Domain.Models.Items.Rarity.Epic,
                Domain.Models.Items.Rarity.Unique,
                Domain.Models.Items.Rarity.Unique,
                Domain.Models.Items.Rarity.Unique,
                Domain.Models.Items.Rarity.Legendary
            ],
            floors.Select(floor => floor.BalanceBenchmark.EquipmentRarity));
        Assert.All(floors, floor => Assert.Equal(1, floor.BalanceBenchmark.EquipmentTier));
        Assert.Equal(
            [152, 154, 156, 162, 163, 166, 170, 171, 175, 179],
            floors.Select(floor => floor.RecommendedPowerRating));
    }

    [Fact]
    public void SoftPowerRewardCurve_ReachesExpectedCheckpointsAndIncreasesEveryFloor()
    {
        var curve = new TowerRewardCurveDefinition();
        var rewards = Enumerable.Range(1, curve.MaximumFloor)
            .Select(curve.Calculate)
            .ToArray();

        Assert.Equal(100, rewards[0]);
        Assert.Equal(122, rewards[9]);
        Assert.Equal(148, rewards[24]);
        Assert.Equal(185, rewards[49]);
        Assert.Equal(219, rewards[74]);
        Assert.Equal(250, rewards[99]);
        Assert.True(rewards.Zip(rewards.Skip(1), (current, next) => next > current).All(x => x));
    }

    [Fact]
    public void FirstClearIsImmutableAndCompletesScouting()
    {
        var unlockedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var clearedAt = unlockedAt.AddDays(1);
        var attemptId = Guid.NewGuid();
        var progress = new TowerFloorProgress
        {
            UnlockedAt = unlockedAt,
            ScoutingProgress = 45,
            CreatedAt = unlockedAt,
            UpdatedAt = unlockedAt
        };

        Assert.True(progress.RecordFirstClear(attemptId, clearedAt));
        Assert.False(progress.RecordFirstClear(Guid.NewGuid(), clearedAt.AddHours(1)));
        Assert.True(progress.IsCleared);
        Assert.Equal(100, progress.ScoutingProgress);
        Assert.Equal(attemptId, progress.FirstClearAttemptId);
        Assert.Equal(clearedAt, progress.ClearedAt);
    }

    [Fact]
    public void ScoutingProgressIsCappedAndCannotRegressAfterClear()
    {
        var now = DateTimeOffset.UtcNow;
        var progress = new TowerFloorProgress { ScoutingProgress = 95 };

        progress.AddScoutingProgress(10, now);
        Assert.Equal(100, progress.ScoutingProgress);

        progress.RecordFirstClear(Guid.NewGuid(), now);
        progress.AddScoutingProgress(0, now.AddMinutes(1));
        Assert.Equal(100, progress.ScoutingProgress);
        Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddScoutingProgress(-1, now));
    }
}
