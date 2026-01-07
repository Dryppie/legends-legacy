using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures;

public class CreatureDefenseProfile
{
    public DefenseProfile Type { get; init; }
    public float PhysicalDefenseBias { get; init; } = 1.0f;
    public float MagicalDefenseBias { get; init; } = 1.0f;
    public float ResistBias { get; init; } = 1.0f;
}