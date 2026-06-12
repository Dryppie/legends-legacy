namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityScalingFormula
{
    public double BaseValue { get; set; }
    public List<AbilityAttributeScalingDefinition> AttributeScaling { get; set; } = [];
}
