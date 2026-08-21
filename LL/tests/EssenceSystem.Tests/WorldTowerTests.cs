using System.Text.Json;
using System.Text.Json.Serialization;
using Application.MediatR.Attributes;
using Application.UseCases.WorldTower;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed class WorldTowerTests
{
    [Fact]
    public void Participant_scaling_grows_health_faster_than_pressure()
    {
        var fivePlayerGuardian = CreateGuardian();
        var tenPlayerGuardian = CreateGuardian();

        WorldTowerGuardianScaling.Apply(fivePlayerGuardian, new TowerGuardianScalingDefinition(), 5);
        WorldTowerGuardianScaling.Apply(tenPlayerGuardian, new TowerGuardianScalingDefinition(), 10);
        AttributeCalculator.CalculateBaseCombatAttributes(fivePlayerGuardian);
        AttributeCalculator.CalculateBaseCombatAttributes(tenPlayerGuardian);

        var healthRatio = tenPlayerGuardian.GetAttributeValue(AttributeType.MaxHealth)
                          / (float)fivePlayerGuardian.GetAttributeValue(AttributeType.MaxHealth);
        var offenseRatio = tenPlayerGuardian.GetAttributeValue(AttributeType.Power)
                           / (float)fivePlayerGuardian.GetAttributeValue(AttributeType.Power);

        Assert.InRange(healthRatio, 1.7f, 1.9f);
        Assert.InRange(offenseRatio, 1.1f, 1.3f);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(5, 1)]
    [InlineData(10, 2)]
    [InlineData(15, 3)]
    public void PartyCountUsesAtMostFiveSlotsPerParty(int requiredSlots, int expectedParties)
    {
        Assert.Equal(expectedParties, WorldTowerPartyRules.GetPartyCount(requiredSlots));
    }

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
        Assert.Equal(Enumerable.Range(1, 10), floors.Select(x => x.ProgressionPosition));
        Assert.Equal([5, 5, 5, 5, 10, 5, 5, 10, 10, 15], floors.Select(x => x.RequiredSlots));
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
        Assert.All(floors, floor => Assert.Equal(floor.TowerTokens * 4, floor.FirstClearTowerTokens));
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
        Assert.Equal(5, floors[6].RequiredSlots);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000063"), floors[7].GuardianCreatureId);
        Assert.Equal("Kodoku, the Poisoned Vessel", floors[7].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000062"), floors[8].GuardianCreatureId);
        Assert.Equal("Ni, the Ninefold", floors[8].GuardianName);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000065"), floors[9].GuardianCreatureId);
        Assert.Equal("The Mad King", floors[9].GuardianName);
        Assert.Equal("monster.the_mad_king", floors[9].GuardianAbilityProfileId);
        Assert.Contains(
            floors.Single(x => x.FloorNumber == 1).Unlocks,
            unlock => unlock.Key == "tower_echo_mode_unlock"
                      && unlock.Description.Contains("Echo Mode", StringComparison.Ordinal));
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

    private static CombatEntity CreateGuardian() =>
        new(new Character
        {
            BaseAttributes =
            [
                new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 },
                new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
                new EntityAttribute { AttributeType = AttributeType.Armor, Value = 10 },
                new EntityAttribute { AttributeType = AttributeType.Resistance, Value = 10 }
            ]
        });
}
