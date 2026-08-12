using System.Text.Json;
using System.Text.Json.Serialization;
using Application.MediatR.Attributes;
using Application.UseCases.WorldTower;
using Domain.Models.WorldTower;
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
    public void CatalogDefinesTenContiguousFloorsUsingExistingCreatures()
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

        Assert.Equal(Enumerable.Range(1, 10), floors.Select(x => x.FloorNumber));
        Assert.Equal([4, 5, 3, 5, 10, 5, 3, 10, 10, 15], floors.Select(x => x.RequiredSlots));
        Assert.All(floors, floor => Assert.Contains(floor.GuardianCreatureId, creatureIds));
        Assert.All(floors, floor => Assert.True(floor.GuardianStrengthMultiplier > 0));
        Assert.All(floors, floor => Assert.True(floor.RecommendedPowerRating >= 0));
        Assert.Contains("tower_echo_mode_unlock", floors.Single(x => x.FloorNumber == 5).UnlockKeys);
        Assert.Contains("tower_band_2_unlock", floors.Single(x => x.FloorNumber == 10).UnlockKeys);
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
