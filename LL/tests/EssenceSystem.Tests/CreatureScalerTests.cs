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
    public void Unified_curve_tracks_canonical_player_growth_across_the_campaign()
    {
        var provider = CreateContentProvider();

        var campaignStart = provider.GetScaling(new Area { Id = "region_01_area_01" });
        var bloodGrove = provider.GetScaling(new Area { Id = "region_01_area_02" });
        var regionOneMid = provider.GetScaling(new Area { Id = "region_01_area_06" });
        var regionOneEnd = provider.GetScaling(new Area { Id = "region_01_area_07" });
        var regionTwoStart = provider.GetScaling(new Area { Id = "region_02_area_01" });
        var rotgraveFields = provider.GetScaling(new Area { Id = "region_02_area_02" });
        var tempestAerie = provider.GetScaling(new Area { Id = "region_02_area_03" });
        var wolfsbaneReach = provider.GetScaling(new Area { Id = "region_02_area_04" });
        var campaignEnd = provider.GetScaling(new Area { DifficultyTier = 100 });
        var unifiedCurve = provider.GetCatalog().Profiles.Single(profile =>
            profile.Id == "unified-global-v1");

        Assert.Equal(1.7, campaignStart.HealthMultiplier, 5);
        Assert.Equal(1.87, campaignStart.OffenseMultiplier, 5);
        Assert.Equal(1.105, campaignStart.DefenseMultiplier, 5);
        Assert.Equal(1.105, campaignStart.ResistanceMultiplier, 5);
        Assert.Equal(2.29, bloodGrove.HealthMultiplier, 5);
        Assert.Equal(4.511, bloodGrove.OffenseMultiplier, 5);
        Assert.Equal(1.491, bloodGrove.DefenseMultiplier, 5);
        Assert.Equal(1.491, bloodGrove.ResistanceMultiplier, 5);
        Assert.Equal(2.31754451, regionOneMid.HealthMultiplier, 5);
        Assert.Equal(4.511, regionOneMid.OffenseMultiplier, 5);
        Assert.Equal(10, regionOneEnd.GlobalStep);
        Assert.Equal(4.40051265, regionOneEnd.HealthMultiplier, 5);
        Assert.Equal(8.93384064, regionOneEnd.OffenseMultiplier, 5);
        Assert.Equal(11, regionTwoStart.GlobalStep);
        Assert.Equal(12, rotgraveFields.GlobalStep);
        Assert.Equal(13, tempestAerie.GlobalStep);
        Assert.Equal(14, wolfsbaneReach.GlobalStep);
        Assert.Equal(354, wolfsbaneReach.RecommendedCombatRating);
        Assert.Equal(5.96, rotgraveFields.HealthMultiplier, 5);
        Assert.Equal(14.3, rotgraveFields.OffenseMultiplier, 5);
        Assert.Equal(2.73, rotgraveFields.DefenseMultiplier, 5);
        Assert.True(regionTwoStart.HealthMultiplier > regionOneEnd.HealthMultiplier);
        Assert.True(regionTwoStart.DefenseMultiplier > regionOneEnd.DefenseMultiplier);
        Assert.Equal(11, unifiedCurve.OffenseCurve.LinearAfterStep);
        Assert.NotNull(unifiedCurve.OffenseCurve.LinearGrowthPerStep);
        Assert.Equal(0.835593870808, unifiedCurve.OffenseCurve.LinearGrowthPerStep.Value, 10);
        Assert.Equal("content-foundation-v1", campaignEnd.ProfileId);
        Assert.Equal(30.49, campaignEnd.OffenseMultiplier, 2);
    }

    [Fact]
    public void Rotgrave_feral_ghoul_has_level_appropriate_health_and_offense()
    {
        var creature = new Creature
        {
            Name = "Feral Ghoul",
            Archetype = CreatureArchetype.Bruiser,
            DamageProfile = DamageProfile.Physical,
            DefenseProfile = DefenseProfile.Balanced,
            BaseLevel = 55,
            Tier = 2
        };

        new CreatureScaler(CreateContentProvider()).ApplyScaling(
            creature,
            new Area { Id = "region_02_area_02" });

        Assert.Equal(429.12f, creature.BaseAttributesDict[AttributeType.MaxHealth], 2);
        Assert.Equal(90.09f, creature.BaseAttributesDict[AttributeType.Power], 2);
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
