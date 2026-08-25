namespace Domain.Models.Professions.Crafting;

public static class CraftingMasteryProgression
{
    public const int ExperiencePerCraft = 25;
    public const int MaxLevel = 100;

    private const int BaseExperienceForFirstLevel = 200;
    private const double GrowthFactor = 1.07;

    public static int GetLevelForExperience(int experience)
        => GetProgressForExperience(experience).Level;

    public static int GetLevelBeforeBulkCraft(
        int startingExperience,
        int totalExperienceGained,
        int craftIndex,
        int craftQuantity)
    {
        if (craftQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(craftQuantity));
        if (craftIndex < 0 || craftIndex >= craftQuantity)
            throw new ArgumentOutOfRangeException(nameof(craftIndex));

        var experienceFromCompletedCrafts = (int)(
            (long)Math.Max(0, totalExperienceGained) * craftIndex / craftQuantity);

        return GetLevelForExperience(Math.Max(0, startingExperience) + experienceFromCompletedCrafts);
    }

    public static CraftingMasteryProgress GetProgressForExperience(int experience)
    {
        var remaining = Math.Max(0, experience);
        var level = 0;

        while (level < MaxLevel)
        {
            var required = GetExperienceRequiredForNextLevel(level);
            if (remaining < required) break;

            remaining -= required;
            level++;
        }

        return level >= MaxLevel
            ? new CraftingMasteryProgress(MaxLevel, 0, 0)
            : new CraftingMasteryProgress(
                level,
                remaining,
                GetExperienceRequiredForNextLevel(level));
    }

    public static int GetExperienceRequiredForNextLevel(int currentLevel)
    {
        var normalizedLevel = Math.Clamp(currentLevel, 0, MaxLevel - 1);
        return (int)Math.Round(
            BaseExperienceForFirstLevel * Math.Pow(GrowthFactor, normalizedLevel),
            MidpointRounding.AwayFromZero);
    }
}

public readonly record struct CraftingMasteryProgress(
    int Level,
    int Experience,
    int ExperienceRequiredForNextLevel);
