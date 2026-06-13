namespace Domain.Models.AbilityDefinitions;

public static class AbilityDefinitionKind
{
    public const string Active = "Active";
    public const string Passive = "Passive";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Active,
        Passive
    };
}
