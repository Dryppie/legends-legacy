using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public static class DamageProfiles
{
    public static readonly DamageProfileConfig Physical = new()
    {
        Type = DamageProfile.Physical,
        PhysicalBias = 1.2f,
        MagicalBias = 0.3f,
        CritBias = 1.0f,
        PenBias = 1.2f,
    };

    public static readonly DamageProfileConfig Magical = new()
    {
        Type = DamageProfile.Magical,
        PhysicalBias = 0.3f,
        MagicalBias = 1.2f,
        CritBias = 1.0f,
        PenBias = 1.2f,
    };

    public static readonly DamageProfileConfig Hybrid = new()
    {
        Type = DamageProfile.Hybrid,
        PhysicalBias = 1.0f,
        MagicalBias = 1.0f,
        CritBias = 1.1f,
        PenBias = 1.0f,
    };

    public static DamageProfileConfig Get(DamageProfile type) => type switch
    {
        DamageProfile.Physical => Physical,
        DamageProfile.Magical => Magical,
        DamageProfile.Hybrid => Hybrid,
        _ => Physical
    };
}
