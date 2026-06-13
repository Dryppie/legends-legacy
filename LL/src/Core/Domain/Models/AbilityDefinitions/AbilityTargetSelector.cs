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
    public const string EveryoneButYou = "EveryoneButYou";
    public const string TwoEnemies = "TwoEnemies";
    public const string TwoAllies = "TwoAllies";
    public const string HighestMaxHealthAlly = "HighestMaxHealthAlly";
    public const string AllyHighestMaxHealth = "AllyHighestMaxHealth";
    public const string Attacker = "Attacker";
    public const string DamageSource = "DamageSource";
    public const string AbilityUser = "AbilityUser";
    public const string SummonedAllies = "SummonedAllies";
    public const string NonSummonedAllies = "NonSummonedAllies";

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
        AllAllies,
        EveryoneButYou,
        TwoEnemies,
        TwoAllies,
        HighestMaxHealthAlly,
        AllyHighestMaxHealth,
        Attacker,
        DamageSource,
        AbilityUser,
        SummonedAllies,
        NonSummonedAllies
    };
}
