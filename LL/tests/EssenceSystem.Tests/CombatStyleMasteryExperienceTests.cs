using Domain.Models.CombatStyles;
using Microsoft.Extensions.Configuration;
using Services.LL.Levels;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CombatStyleMasteryExperienceTests
{
    [Fact]
    public void Each_mastery_costs_its_ten_combat_level_requirements()
    {
        var combat = new JsonCharacterExperienceProgressionProvider(
            new ConfigurationBuilder().Build(), TestContentPaths.FindApiRoot(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var requirements = CombatStyleFoundationTests.LoadCatalog().XpRequirements;
        Assert.Equal(10, requirements.Count);

        for (var mastery = 1; mastery <= 10; mastery++)
        {
            var expected = Enumerable.Range((mastery - 1) * 10 + 1, 10)
                .Sum(combat.GetRequiredExperience);
            Assert.Equal(expected, requirements[mastery - 1]);
            Assert.Equal(expected, CombatStyleProgression.XpRequired(mastery - 1, requirements));
        }
    }

    [Fact]
    public void Every_mastery_requires_its_full_band_and_cap_discards_overflow()
    {
        var requirements = CombatStyleFoundationTests.LoadCatalog().XpRequirements;
        var style = new CharacterCombatStyle();
        Assert.Equal(0, style.Level);

        for (var mastery = 1; mastery <= 10; mastery++)
        {
            var before = CombatStyleProgression.Grant(style, requirements[mastery - 1] - 1, requirements);
            Assert.Equal(mastery - 1, before.Level);
            Assert.Equal(0, before.LevelsGained);
            var earned = CombatStyleProgression.Grant(style, 1, requirements);
            Assert.Equal(mastery, earned.Level);
            Assert.Equal(1, earned.LevelsGained);
            Assert.Equal(0, earned.CurrentXp);
        }

        Assert.Equal(0, CombatStyleProgression.XpRequired(10, requirements));
        Assert.Equal(0, CombatStyleProgression.Grant(style, long.MaxValue, requirements).XpGained);
        var single = new CharacterCombatStyle();
        var all = CombatStyleProgression.Grant(single, long.MaxValue, requirements);
        Assert.Equal(requirements.Sum(), all.XpGained);
        Assert.Equal(10, all.LevelsGained);
        Assert.True(all.ReachedCap);
    }

    [Fact]
    public void Existing_mastery_and_partial_experience_are_preserved()
    {
        var requirements = CombatStyleFoundationTests.LoadCatalog().XpRequirements;
        var style = new CharacterCombatStyle { Level = 5, CurrentXp = 123 };
        var result = CombatStyleProgression.Grant(style, 100, requirements);
        Assert.Equal(5, result.Level);
        Assert.Equal(223, result.CurrentXp);
        Assert.Equal(requirements[5], CombatStyleProgression.XpRequired(style.Level, requirements));
    }
}
