using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.RegionBosses;
using Microsoft.Extensions.Configuration;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossDefinitionProviderTests
{
    [Fact]
    public void Catalog_contains_only_the_mad_king_using_the_floor_ten_guardian_and_unlock()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var provider = new JsonRegionBossDefinitionProvider(configuration, apiRoot, options);
        using var towerDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(apiRoot, "Data", "world-tower", "tower-floors.json")));
        var floorTen = towerDocument.RootElement.GetProperty("floors").EnumerateArray()
            .Single(x => x.GetProperty("floorNumber").GetInt32() == 10);

        var boss = Assert.Single(provider.GetAll());

        Assert.Equal("region-1-mad-king", boss.Id);
        Assert.Equal(1, boss.RegionId);
        Assert.Equal(floorTen.GetProperty("guardianCreatureId").GetGuid(), boss.CreatureId);
        Assert.Equal(floorTen.GetProperty("guardianName").GetString(), boss.Name);
        Assert.Equal(10, boss.RequiredTowerFloor);
        Assert.Equal(1, boss.LevelRequirement);
        Assert.Equal(250, boss.BaseScaling.Health);
        Assert.Equal(20, boss.BaseScaling.Power);
        Assert.Equal(5, boss.BaseScaling.Armor);
        Assert.Equal(5, boss.BaseScaling.Resistance);
        Assert.Equal(18, boss.BaseScaling.Penetration);
        Assert.Equal(RegionBossGrowthCurve.ShiftedPower, boss.LevelScaling.GrowthCurve);
        Assert.Equal(0.75, boss.LevelScaling.HealthGrowth);
        Assert.Equal(1.50, boss.LevelScaling.HealthGrowthExponent);
        Assert.Equal(0.30, boss.LevelScaling.PowerGrowth);
        Assert.Equal(1.20, boss.LevelScaling.PowerGrowthExponent);
        Assert.Equal(0.12, boss.LevelScaling.ArmorGrowthPerLevel);
        Assert.Equal(0.12, boss.LevelScaling.ResistanceGrowthPerLevel);
        Assert.Equal(0.10, boss.LevelScaling.PenetrationGrowthPerLevel);
        Assert.Equal(15, boss.Revival.BaseDelaySeconds);
        Assert.Equal(50, boss.Revival.ReviveHealthPercent);
        Assert.Equal(50, boss.Recovery.DownedReviveHealthPercent);
        Assert.Equal(4, boss.Schedule.MinimumIntervalHours);
        Assert.Equal(8, boss.Schedule.MaximumIntervalHours);
        Assert.False(boss.RewardsEnabled);
        Assert.Empty(boss.RewardBrackets);
    }

    [Fact]
    public void Loads_valid_catalog_and_supports_case_insensitive_lookup()
    {
        using var catalog = Catalog.Create(CreateDefinition());

        var provider = catalog.CreateProvider();

        Assert.Single(provider.GetAll());
        Assert.Equal("Test Boss", provider.Get("TEST-BOSS")?.Name);
    }

    [Fact]
    public void Rejects_non_positive_base_scaling()
    {
        using var catalog = Catalog.Create(CreateDefinition(baseHealth: 0));

        var exception = Assert.Throws<InvalidOperationException>(catalog.CreateProvider);

        Assert.Contains("invalid combat scaling", exception.Message);
    }

    [Fact]
    public void Rejects_invalid_tower_requirement()
    {
        using var catalog = Catalog.Create(CreateDefinition(requiredTowerFloor: 0));

        var exception = Assert.Throws<InvalidOperationException>(catalog.CreateProvider);

        Assert.Contains("invalid identity or access settings", exception.Message);
    }

    [Fact]
    public void Rejects_duplicate_reward_milestones()
    {
        var rewards = new[]
        {
            new RegionBossRewardBracketDefinition { Key = "first", MinimumLevelDefeated = 1, Cinders = 10 },
            new RegionBossRewardBracketDefinition { Key = "second", MinimumLevelDefeated = 1, Soulstones = 5 }
        };
        using var catalog = Catalog.Create(CreateDefinition(rewards: rewards));

        var exception = Assert.Throws<InvalidOperationException>(catalog.CreateProvider);

        Assert.Contains("invalid reward brackets", exception.Message);
    }

    [Fact]
    public void Rejects_a_schedule_with_an_inverted_spawn_interval()
    {
        using var catalog = Catalog.Create(CreateDefinition(schedule: new RegionBossScheduleDefinition
        {
            MinimumIntervalHours = 8,
            MaximumIntervalHours = 4,
            SignupDurationMinutes = 10
        }));

        var exception = Assert.Throws<InvalidOperationException>(catalog.CreateProvider);

        Assert.Contains("invalid schedule settings", exception.Message);
    }

    private static RegionBossDefinition CreateDefinition(
        double baseHealth = 1,
        int? requiredTowerFloor = null,
        IReadOnlyList<RegionBossRewardBracketDefinition>? rewards = null,
        RegionBossScheduleDefinition? schedule = null) =>
        new()
        {
            Id = "test-boss",
            Name = "Test Boss",
            ImagePath = "test_boss",
            RegionId = 1,
            CreatureId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            LevelRequirement = 10,
            RequiredTowerFloor = requiredTowerFloor,
            BaseScaling = new RegionBossBaseScalingDefinition { Health = baseHealth },
            Schedule = schedule ?? new RegionBossScheduleDefinition(),
            RewardsEnabled = true,
            RewardBrackets = rewards ??
            [
                new RegionBossRewardBracketDefinition
                {
                    Key = "first",
                    MinimumLevelDefeated = 1,
                    Cinders = 10
                }
            ]
        };

    private sealed class Catalog : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string root;

        private Catalog(string root) => this.root = root;

        public static Catalog Create(params RegionBossDefinition[] definitions)
        {
            var root = Path.Combine(Path.GetTempPath(), "legends-legacy-region-boss-tests", Guid.NewGuid().ToString("N"));
            var directory = Path.Combine(root, "Data", "region-bosses");
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "region-bosses.json"),
                JsonSerializer.Serialize(new RegionBossCatalogDocument { RegionBosses = definitions }, JsonOptions));
            return new Catalog(root);
        }

        public JsonRegionBossDefinitionProvider CreateProvider()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
                .Build();
            return new JsonRegionBossDefinitionProvider(configuration, root, JsonOptions);
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
