namespace Domain.Models.Professions.Crafting;

public static class CraftingMasteryProgression
{
    public const int ExperiencePerCraft = 25;
    public const int MaxLevel = 100;

    private const int BaseExperienceForFirstLevel = 200;
    private const double GrowthFactor = 1.15;

    public static int GetLevelForExperience(int experience)
    {
        if (experience <= 0) return 0;

        var remaining = experience;
        var level = 0;

        while (level < MaxLevel)
        {
            var required = GetExperienceRequiredForNextLevel(level);
            if (remaining < required) break;

            remaining -= required;
            level++;
        }

        return level;
    }

    public static int GetExperienceRequiredForNextLevel(int currentLevel)
    {
        var normalizedLevel = Math.Clamp(currentLevel, 0, MaxLevel - 1);
        return (int)Math.Round(
            BaseExperienceForFirstLevel * Math.Pow(GrowthFactor, normalizedLevel),
            MidpointRounding.AwayFromZero);
    }
}
