using Domain.Models.Dungeons.Mastery;

namespace EssenceSystem.Tests;

public sealed class DungeonMasteryBenefitsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0.00, 0, 0)]
    [InlineData(1, 1, 0, 0.00, 0, 0)]
    [InlineData(2, 1, 2, 0.00, 0, 0)]
    [InlineData(3, 1, 2, 0.05, 0, 0)]
    [InlineData(4, 1, 2, 0.05, 1, 0)]
    [InlineData(5, 1, 2, 0.05, 1, 10)]
    [InlineData(6, 2, 2, 0.05, 1, 10)]
    [InlineData(7, 2, 4, 0.05, 1, 10)]
    [InlineData(8, 2, 4, 0.10, 1, 10)]
    [InlineData(9, 2, 4, 0.10, 2, 10)]
    [InlineData(10, 2, 4, 0.10, 2, 10)]
    public void Resolve_returns_the_cumulative_benefits_for_each_breakpoint(
        int level,
        int visibility,
        int restBonus,
        double gatheringBonus,
        int vigorReduction,
        int currencyBonus)
    {
        var benefits = DungeonMasteryBenefits.Resolve(level);

        Assert.Equal(visibility, benefits.AdditionalVisibilityRows);
        Assert.Equal(restBonus, benefits.RestSiteVigorBonus);
        Assert.Equal(vigorReduction, benefits.CombatVigorCostReduction);
        Assert.Equal(currencyBonus, benefits.CompletionCurrencyBonusPercent);
    }

    [Fact]
    public void Definitions_cover_current_mastery_benefits_once()
    {
        Assert.Equal(
            new[] { 1, 2, 4, 5, 6, 7, 9, 10 },
            DungeonMasteryBenefits.Definitions.Select(benefit => benefit.Level));
        Assert.Equal(
            DungeonMasteryBenefits.Definitions.Count,
            DungeonMasteryBenefits.Definitions.Select(benefit => benefit.Id).Distinct().Count());
    }
}
