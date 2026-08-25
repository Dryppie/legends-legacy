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

    [Fact]
    public void GetProgressForExperience_ReturnsExperienceWithinCurrentLevel()
    {
        var firstLevel = CraftingMasteryProgression.GetExperienceRequiredForNextLevel(0);
        var secondLevel = CraftingMasteryProgression.GetExperienceRequiredForNextLevel(1);

        var progress = CraftingMasteryProgression.GetProgressForExperience(firstLevel + 25);

        Assert.Equal(1, progress.Level);
        Assert.Equal(25, progress.Experience);
        Assert.Equal(secondLevel, progress.ExperienceRequiredForNextLevel);
    }

    [Fact]
    public void GetLevelBeforeBulkCraft_UsesLevelsEarnedEarlierInTheBatch()
    {
        const int craftQuantity = 3;
        var startingExperience = CraftingMasteryProgression.GetExperienceRequiredForNextLevel(0) -
                                 CraftingMasteryProgression.ExperiencePerCraft;
        var totalExperienceGained = craftQuantity * CraftingMasteryProgression.ExperiencePerCraft;

        var levels = Enumerable.Range(0, craftQuantity)
            .Select(index => CraftingMasteryProgression.GetLevelBeforeBulkCraft(
                startingExperience,
                totalExperienceGained,
                index,
                craftQuantity))
            .ToList();

        Assert.Equal([0, 1, 1], levels);
    }

    [Fact]
    public void GetLevelBeforeBulkCraft_DistributesTheAwardedBatchExperienceWithoutLosingRemainders()
    {
        const int craftQuantity = 3;
        const int totalExperienceGained = 80;
        const int startingExperience = 147;

        var levels = Enumerable.Range(0, craftQuantity)
            .Select(index => CraftingMasteryProgression.GetLevelBeforeBulkCraft(
                startingExperience,
                totalExperienceGained,
                index,
                craftQuantity))
            .ToList();

        Assert.Equal([0, 0, 1], levels);
    }
}
