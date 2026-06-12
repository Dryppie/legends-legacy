namespace Domain.Models.AbilityDefinitions;

public static class AbilityConditionType
{
    public const string SourceHasTag = "SourceHasTag";
    public const string TargetHasTag = "TargetHasTag";
    public const string TargetHasStatus = "TargetHasStatus";
    public const string SourceHasStatus = "SourceHasStatus";
    public const string TargetHealthBelowPercent = "TargetHealthBelowPercent";
    public const string SourceHealthBelowPercent = "SourceHealthBelowPercent";
    public const string IsSpecies = "IsSpecies";
    public const string RandomChance = "RandomChance";
    public const string CooldownReady = "CooldownReady";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SourceHasTag,
        TargetHasTag,
        TargetHasStatus,
        SourceHasStatus,
        TargetHealthBelowPercent,
        SourceHealthBelowPercent,
        IsSpecies,
        RandomChance,
        CooldownReady
    };
}
