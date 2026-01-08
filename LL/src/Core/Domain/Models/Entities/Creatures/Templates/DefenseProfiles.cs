using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public static class DefenseProfiles
{
    public static readonly DefenseProfileConfig Balanced = new()
    {
        Type = DefenseProfile.Balanced
    };

    public static readonly DefenseProfileConfig PhysicalTank = new()
    {
        Type = DefenseProfile.PhysicalTank,
        PhysicalDefenseBias = 1.2f,
        MagicalDefenseBias = 0.4f,
        ResistBias = 1.0f
    };

    public static readonly DefenseProfileConfig MagicalTank = new()
    {
        Type = DefenseProfile.MagicalTank,
        PhysicalDefenseBias = 0.4f,
        MagicalDefenseBias = 1.2f,
        ResistBias = 1.0f
    };

    public static readonly DefenseProfileConfig ElementalTank = new()
    {
        Type = DefenseProfile.ElementalTank,
        PhysicalDefenseBias = 1.0f,
        MagicalDefenseBias = 1.0f,
        ResistBias = 1.6f
    };

    public static DefenseProfileConfig Get(DefenseProfile type) => type switch
    {
        DefenseProfile.Balanced => Balanced,
        DefenseProfile.PhysicalTank => PhysicalTank,
        DefenseProfile.MagicalTank => MagicalTank,
        DefenseProfile.ElementalTank => ElementalTank,
        _ => Balanced
    };
}
