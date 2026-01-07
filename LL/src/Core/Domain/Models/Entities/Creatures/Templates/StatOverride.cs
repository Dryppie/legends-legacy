using Domain.Models.Attributes;

namespace Domain.Models.Entities.Creatures.Templates;

public sealed class StatOverride
{
    public Guid Id { get; set; }
    public AttributeType AttributeType { get; set; }
    public float? Multiplier { get; set; }
    public float? Additive { get; set; }
}
