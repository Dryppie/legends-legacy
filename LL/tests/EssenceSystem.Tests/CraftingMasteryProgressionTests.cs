using Domain.Models.Professions.Crafting;

namespace EssenceSystem.Tests;

public sealed class CraftingMasteryProgressionTests
{
    [Fact]
    public void GetLevelForExperience_UsesExponentialThresholds()
    {
        var levelOneRequired = CraftingMasteryProgression.GetExperienceRequiredForNextLevel(0);
        var levelTwoRequired = CraftingMasteryProgression.GetExperienceRequiredForNextLevel(1);

        Assert.Equal(200, levelOneRequired);
        Assert.True(levelTwoRequired > levelOneRequired);
        Assert.Equal(0, CraftingMasteryProgression.GetLevelForExperience(100));
        Assert.Equal(1, CraftingMasteryProgression.GetLevelForExperience(levelOneRequired));
        Assert.Equal(2, CraftingMasteryProgression.GetLevelForExperience(levelOneRequired + levelTwoRequired));
    }
}
