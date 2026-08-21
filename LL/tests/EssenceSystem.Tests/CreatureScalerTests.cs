using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Entities.Creatures;
using Services.LL.Regions;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CreatureScalerTests
{
    [Fact]
    public void ApplyScaling_applies_training_creature_stat_overrides()
    {
        var creature = CreateBalancedCreature();
        creature.StatOverrides =
        [
            new StatOverride { AttributeType = AttributeType.MaxHealth, Multiplier = 0.20f },
            new StatOverride { AttributeType = AttributeType.Power, Multiplier = 0.10f },
            new StatOverride { AttributeType = AttributeType.Armor, Multiplier = 0.50f }
        ];
        var area = new Area { Name = "Training Area", LevelRequirement = 1, DifficultyTier = 0 };

        new CreatureScaler().ApplyScaling(creature, area);

        Assert.Equal(12, creature.BaseAttributesDict[AttributeType.MaxHealth]);
        Assert.Equal(0.7f, creature.BaseAttributesDict[AttributeType.Power], 3);
        Assert.Equal(5, creature.BaseAttributesDict[AttributeType.Armor]);
    }

    [Fact]
    public void Unified_curve_applies_area_growth_and_a_distinct_region_jump()
    {
        var provider = CreateContentProvider();

        var regionOneEnd = provider.GetScaling(new Area { Id = "region_01_area_07" });
        var regionTwoStart = provider.GetScaling(new Area { Id = "region_02_area_01" });
        var campaignEnd = provider.GetScaling(new Area { DifficultyTier = 100 });

        Assert.Equal(10, regionOneEnd.GlobalStep);
        Assert.Equal(11, regionTwoStart.GlobalStep);
        Assert.Equal(1.12, regionTwoStart.OffenseMultiplier / regionOneEnd.OffenseMultiplier, 5);
        Assert.True(regionTwoStart.HealthMultiplier > regionOneEnd.HealthMultiplier);
        Assert.True(regionTwoStart.DefenseMultiplier > regionOneEnd.DefenseMultiplier);
        Assert.Equal(16.48, campaignEnd.OffenseMultiplier / 1.85, 2);
    }

    [Fact]
    public void Unified_curve_adds_percentage_point_budgets_to_zero_baseline_secondaries()
    {
        var creature = CreateBalancedCreature();

        new CreatureScaler(CreateContentProvider()).ApplyScaling(
            creature,
            new Area { Id = "region_02_area_02" });

        Assert.True(creature.BaseAttributesDict[AttributeType.AttackSpeed] > 0);
        Assert.True(creature.BaseAttributesDict[AttributeType.ArmorPenetration] > 0);
        Assert.True(creature.BaseAttributesDict[AttributeType.DamageReduction] > 0);
        Assert.True(creature.BaseAttributesDict[AttributeType.CritChance] >= 5);
        Assert.True(creature.BaseAttributesDict[AttributeType.CritDamage] >= 50);
    }

    private static RegionCreatureScalingProvider CreateContentProvider()
    {
        var apiRoot = Environment.GetEnvironmentVariable("LL_TEST_API_ROOT")
            ?? TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();

        return new RegionCreatureScalingProvider(
            configuration,
            apiRoot,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static Creature CreateBalancedCreature() =>
        new()
        {
            Name = "Goblin",
            Archetype = CreatureArchetype.Balanced,
            DamageProfile = DamageProfile.Hybrid,
            DefenseProfile = DefenseProfile.Balanced
        };
}
