using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Professions.Gathering;

public static class GatheringProfessionProgression
{
    public const int MaxLevel = 100;
    public const int ExperiencePerAttempt = 50;
    private const int ExperienceCurveCoefficient = 474;

    public static bool IsGatheringProfession(ProfessionType professionType) => professionType is
        ProfessionType.Mining or
        ProfessionType.Woodcutting or
        ProfessionType.Skinning;

    public static ProfessionType ToProfessionType(GatheringType gatheringType) => gatheringType switch
    {
        GatheringType.Mining => ProfessionType.Mining,
        GatheringType.Woodcutting => ProfessionType.Woodcutting,
        GatheringType.Skinning => ProfessionType.Skinning,
        _ => ProfessionType.None
    };

    public static int GetRequiredExperience(int currentLevel)
    {
        if (currentLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLevel));
        }

        if (currentLevel >= MaxLevel)
        {
            return 0;
        }

        return checked(ExperienceCurveCoefficient * currentLevel * currentLevel);
    }

    public static long GetCumulativeExperienceForLevel(int targetLevel)
    {
        if (targetLevel is < 1 or > MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLevel));
        }

        var completedLevels = targetLevel - 1L;
        var sumOfSquares = checked(
            completedLevels * targetLevel * ((2L * targetLevel) - 1L) / 6L);
        return checked(ExperienceCurveCoefficient * sumOfSquares);
    }

}
