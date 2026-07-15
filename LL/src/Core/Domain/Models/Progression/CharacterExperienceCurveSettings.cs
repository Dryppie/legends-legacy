namespace Domain.Models.Progression;

public sealed class CharacterExperienceCurveSettings
{
    public int BaseExperience { get; set; }
    public int LinearExperiencePerLevel { get; set; }
    public int QuadraticExperiencePerLevelSquared { get; set; }
    public int RoundingIncrement { get; set; }
}

public static class CharacterExperienceCurve
{
    public static long CalculateRequiredExperience(
        int level,
        CharacterExperienceCurveSettings settings)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "Character level must be positive.");
        }

        var rawExperience = checked(
            (long)settings.BaseExperience +
            (long)settings.LinearExperiencePerLevel * level +
            (long)settings.QuadraticExperiencePerLevelSquared * level * level);
        var roundedExperience = checked(
            ((rawExperience + settings.RoundingIncrement - 1) / settings.RoundingIncrement) *
            settings.RoundingIncrement);

        return roundedExperience;
    }
}
