using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public sealed class DamageProfileConfig
{
    public DamageProfile Type { get; init; }
    public float PhysicalBias { get; init; } = 1.0f;
    public float MagicalBias { get; init; } = 1.0f;
    public float CritBias { get; init; } = 1.0f;
    public float PenBias { get; init; } = 1.0f;
}