namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityModifierDefinition
{
    public string Target { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Condition { get; set; }
    public AbilityEffectDefinition? Effect { get; set; }
}
