using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Regions;
using Services.LL.Spawnings;

namespace EssenceSystem.Tests;

public sealed class RegionAreaBalancingTests
{
    [Fact]
    public void Region_one_uses_explicit_smooth_global_steps()
    {
        var provider = CreateProvider();
        var region = provider.GetCatalog().Regions.Single(x => x.RegionKey == "shenic");

        Assert.Equal(10, region.AreaIds.Count);
        Assert.Equal("region_01_area_01", region.AreaIds[0]);
        Assert.Equal("region_01_area_07", region.AreaIds[^1]);

        var scalings = region.AreaIds
            .Select((areaId, index) => provider.GetScaling(new Area
            {
                Id = areaId,
                DifficultyTier = index + 1
            }))
            .ToArray();

        Assert.Equal(Enumerable.Range(1, 10), scalings.Select(x => x.GlobalStep));
        Assert.All(scalings.Zip(scalings.Skip(1)), pair =>
        {
            Assert.True(pair.Second.HealthMultiplier > pair.First.HealthMultiplier);
            Assert.True(pair.Second.OffenseMultiplier > pair.First.OffenseMultiplier);
            Assert.True(pair.Second.DefenseMultiplier > pair.First.DefenseMultiplier);
        });
    }

    [Fact]
    public void Region_curve_uses_the_shenic_profile_without_changing_the_fallback()
    {
        var provider = CreateProvider();
        var first = provider.GetScaling(new Area { Id = "region_01_area_01", DifficultyTier = 1 });
        var last = provider.GetScaling(new Area { Id = "region_01_area_07", DifficultyTier = 10 });

        Assert.Equal("shenic-area-v1", first.ProfileId);
        Assert.Equal(1d, first.HealthMultiplier, 6);
        Assert.Equal(Math.Pow(1 + 0.22 * 9, 1.12), last.HealthMultiplier, 6);
        Assert.Equal(Math.Pow(1 + 0.18 * 9, 1.10), last.OffenseMultiplier, 6);

        var fallback = provider.GetScaling(new Area { Id = "unmapped", DifficultyTier = 10 });
        Assert.Equal("legacy-area-v1", fallback.ProfileId);
        Assert.Equal(Math.Pow(1 + 0.22 * 9, 1.12), fallback.HealthMultiplier, 6);
        Assert.All(provider.GetCatalog().Profiles, profile => Assert.Equal(8_500, profile.TargetWinRateBasisPoints));
    }

    [Fact]
    public void Uncatalogued_future_area_uses_the_reusable_fallback_curve()
    {
        var provider = CreateProvider();

        var scaling = provider.GetScaling(new Area
        {
            Id = "region_02_area_01",
            DifficultyTier = 11
        });

        Assert.Null(scaling.RegionKey);
        Assert.Equal(11, scaling.GlobalStep);
        Assert.Equal(10, scaling.ProgressionStep);
        Assert.True(scaling.HealthMultiplier > 3);
    }

    [Fact]
    public void Weighted_spawn_selection_is_repeatable_for_a_fixed_seed()
    {
        var creatures = new[]
        {
            new AreaCreature { CreatureId = Guid.Parse("00000000-0000-0000-0000-000000000001"), WeightedSpawnRate = 0.2f },
            new AreaCreature { CreatureId = Guid.Parse("00000000-0000-0000-0000-000000000002"), WeightedSpawnRate = 0.3f },
            new AreaCreature { CreatureId = Guid.Parse("00000000-0000-0000-0000-000000000003"), WeightedSpawnRate = 0.5f }
        };
        float[] probabilities = [0.03f, 0.969f, 0.001f];

        var first = CreateSequence(new Random(123_456));
        var second = CreateSequence(new Random(123_456));

        Assert.Equal(first, second);
        Assert.Contains(first, value => value.StartsWith("2:"));

        string[] CreateSequence(Random random) => Enumerable.Range(0, 100)
            .Select(_ =>
            {
                var count = WeightedSpawnSelector.SelectCreatureCount(probabilities, random);
                var selected = WeightedSpawnSelector.SelectCreatures(creatures, count, random);
                return $"{count}:{string.Join(',', selected.Select(x => x.CreatureId))}";
            })
            .ToArray();
    }

    private static RegionCreatureScalingProvider CreateProvider()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return new RegionCreatureScalingProvider(configuration, apiRoot, options);
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (File.Exists(Path.Combine(candidate, "Data", "progression", "region-combat-balance.json")))
                    return candidate;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the API.LL content root.");
    }
}
