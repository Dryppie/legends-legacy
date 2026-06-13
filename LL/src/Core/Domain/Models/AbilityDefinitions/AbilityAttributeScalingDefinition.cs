using Domain.Models.Attributes;

namespace Domain.Models.AbilityDefinitions;

public sealed class AbilityAttributeScalingDefinition
{
    public AttributeType Attribute { get; set; }
    public double Coefficient { get; set; }
}
