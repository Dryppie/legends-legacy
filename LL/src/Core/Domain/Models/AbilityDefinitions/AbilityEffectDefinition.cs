namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityEffectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Target { get; set; } = AbilityTargetSelector.CurrentTarget;
    public string? Attribute { get; set; }
    public string? Status { get; set; }
    public string? Resource { get; set; }
    public double? DurationSeconds { get; set; }
    public double? IntervalSeconds { get; set; }
    public int? Uses { get; set; }
    public string? AttackType { get; set; }
    public string? DamageType { get; set; }
    public List<string> EffectTags { get; set; } = [];
    public string? Log { get; set; }
    public float LifeStealPercentage { get; set; }
    public AbilityScalingFormula Scaling { get; set; } = new();
    public List<AbilityConditionDefinition> Conditions { get; set; } = [];
}
