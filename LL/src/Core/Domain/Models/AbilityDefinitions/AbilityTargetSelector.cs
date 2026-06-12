namespace Domain.Models.AbilityDefinitions;

public static class AbilityTargetSelector
{
    public const string Self = "Self";
    public const string CurrentTarget = "CurrentTarget";
    public const string RandomEnemy = "RandomEnemy";
    public const string LowestHealthEnemy = "LowestHealthEnemy";
    public const string HighestHealthEnemy = "HighestHealthEnemy";
    public const string LowestHealthAlly = "LowestHealthAlly";
    public const string RandomAlly = "RandomAlly";
    public const string AllEnemies = "AllEnemies";
    public const string AllAllies = "AllAllies";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Self,
        CurrentTarget,
        RandomEnemy,
        LowestHealthEnemy,
        HighestHealthEnemy,
        LowestHealthAlly,
        RandomAlly,
        AllEnemies,
        AllAllies
    };
}
