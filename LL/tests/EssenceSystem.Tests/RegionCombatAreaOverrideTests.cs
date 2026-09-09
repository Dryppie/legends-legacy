using Application.Interfaces.Services.LL.Regions;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions.Areas;
using Microsoft.Extensions.Configuration;
using Services.LL.Entities.Creatures;
using Services.LL.Regions;

namespace EssenceSystem.Tests;

public sealed class RegionCombatAreaOverrideTests
{
    private static RegionCombatBalanceCatalog Catalog => BloodGroveLocalValidation.ReadCandidateCatalog(TestContentPaths.FindApiRoot());

    [Fact]
    public void Production_mapping_changes_only_Blood_Grove_offense_and_coupled_regeneration()
    {
        var catalog = Catalog;
        var original = new RegionCreatureScalingProvider(catalog with { AreaOverrides = null });
        var current = new RegionCreatureScalingProvider(new ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot(), HarnessJson.Options);
        Assert.Equal(HarnessJson.Hash(catalog), HarnessJson.Hash(current.GetCatalog()));
        foreach (var id in catalog.Regions.SelectMany(r => r.AreaIds))
        {
            var area = new Area { Id = id };
            var before = original.GetScaling(area);
            var after = current.GetScaling(area);
            Assert.Equal(id == BloodGroveLocalValidation.AreaId ? before with { OffenseMultiplier = 2.421 } : before, after);
            Creature Create() => new() { Archetype = CreatureArchetype.Balanced, DamageProfile = DamageProfile.Hybrid, DefenseProfile = DefenseProfile.Balanced };
            var a = Create();
            var b = Create();
            new CreatureScaler(original).ApplyScaling(a, area);
            new CreatureScaler(current).ApplyScaling(b, area);
            foreach (var stat in a.BaseAttributesDict.Keys)
                if (id != BloodGroveLocalValidation.AreaId || stat is not (AttributeType.Power or AttributeType.HealthRegeneration))
                    Assert.Equal(a.BaseAttributesDict[stat], b.BaseAttributesDict[stat]);
            if (id == BloodGroveLocalValidation.AreaId)
            {
                Assert.Equal(2.421 / 4.511, b.BaseAttributesDict[AttributeType.Power] / a.BaseAttributesDict[AttributeType.Power], 6);
                Assert.Equal(Math.Sqrt(2.421 / 4.511), b.BaseAttributesDict[AttributeType.HealthRegeneration] / a.BaseAttributesDict[AttributeType.HealthRegeneration], 6);
            }
        }
        Assert.Equal(original.GetScaling(new Area { Id = "not-authored", DifficultyTier = 2 }),
            current.GetScaling(new Area { Id = "not-authored", DifficultyTier = 2 }));
    }

    [Fact]
    public void Local_transition_requires_its_explicit_ceiling_and_does_not_disable_other_guards()
    {
        var catalog = Catalog;
        var bloodGrove = catalog.AreaOverrides!.Single(x => x.AreaId == BloodGroveLocalValidation.AreaId);
        var creek = catalog.AreaOverrides!.Single(x => x.AreaId == "region_01_area_03");
        Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with { AreaOverrides = [bloodGrove] }));
        Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with { AreaOverrides = [bloodGrove, creek with { MaximumOffenseStepIncrease = 0.86 }] }));
        Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with { AreaOverrides = [bloodGrove with { OffenseMultiplier = 1 }, creek] }));
        var region = catalog.Regions[0];
        var profile = catalog.Profiles.Single(p => p.Id == region.ProfileId);
        Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with
        {
            Profiles = catalog.Profiles.Select(p => p.Id == profile.Id ? p with { HealthCurve = p.HealthCurve with { PostTutorialBonus = 5 } } : p).ToArray()
        }));
    }

    [Fact]
    public void Area_overrides_reject_unknown_duplicate_empty_and_nonfinite_settings()
    {
        var catalog = Catalog with { AreaOverrides = null };
        var valid = new RegionCombatAreaOverride("region_01_area_02", "Test", 4.511);
        foreach (var invalid in new[]
        {
            valid with { AreaId = "unknown" }, valid with { Reason = "" },
            valid with { OffenseMultiplier = null }, valid with { OffenseMultiplier = 0 },
            valid with { OffenseMultiplier = double.NaN }, valid with { OffenseMultiplier = double.PositiveInfinity },
            valid with { MaximumOffenseStepIncrease = double.NaN }, valid with { MaximumOffenseStepIncrease = double.PositiveInfinity },
            valid with { MaximumOffenseStepIncrease = -1 }, valid with { AreaId = "region_01_area_01", MaximumOffenseStepIncrease = 1 }
        })
            Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with { AreaOverrides = [invalid] }));
        Assert.Throws<InvalidOperationException>(() => new RegionCreatureScalingProvider(catalog with { AreaOverrides = [valid, valid with { AreaId = valid.AreaId.ToUpperInvariant() }] }));
    }
}
