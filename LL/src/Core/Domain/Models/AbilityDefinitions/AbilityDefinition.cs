namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double CooldownSeconds { get; set; }
    public string Targeting { get; set; } = AbilityTargetSelector.CurrentTarget;
    public List<string> Tags { get; set; } = [];
    public List<AbilityTriggerDefinition> Triggers { get; set; } = [];
    public List<AbilityConditionDefinition> Conditions { get; set; } = [];
    public List<AbilityEffectDefinition> Effects { get; set; } = [];
}
