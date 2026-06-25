namespace Domain.Models.Professions.Crafting.V2;

public static class TemperingConstants
{
    public const int PotentialCost = 1;
    public const int CriticalFailProgress = 0;
    public const int FailProgress = 1;
    public const int SuccessProgress = 3;
    public const int GreatSuccessProgress = 6;

    public static int GetProgressForOutcome(TemperingOutcomeType outcome) => outcome switch
    {
        TemperingOutcomeType.CriticalFail => CriticalFailProgress,
        TemperingOutcomeType.Fail => FailProgress,
        TemperingOutcomeType.Success => SuccessProgress,
        TemperingOutcomeType.GreatSuccess => GreatSuccessProgress,
        _ => 0
    };
}
