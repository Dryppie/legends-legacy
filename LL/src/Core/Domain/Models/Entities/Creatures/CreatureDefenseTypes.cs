using Domain.Models.Entities.Creatures.Templates.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities.Creatures;

public class CreatureDefenseTypes
{
    public static readonly CreatureDefenseProfile Balanced = new()
    {
        Type = DefenseProfile.Balanced
    };

    public static readonly CreatureDefenseProfile PhysicalTank = new()
    {
        Type = DefenseProfile.PhysicalTank,
        PhysicalDefenseBias = 1.5f,
        MagicalDefenseBias = 0.9f,
        ResistBias = 1.0f
    };

    public static readonly CreatureDefenseProfile MagicalTank = new()
    {
        Type = DefenseProfile.MagicalTank,
        PhysicalDefenseBias = 0.9f,
        MagicalDefenseBias = 1.5f,
        ResistBias = 1.2f
    };

    public static readonly CreatureDefenseProfile ElementalTank = new()
    {
        Type = DefenseProfile.ElementalTank,
        PhysicalDefenseBias = 1.0f,
        MagicalDefenseBias = 1.0f,
        ResistBias = 1.6f
    };

    public static CreatureDefenseProfile Get(DefenseProfile type) => type switch
    {
        DefenseProfile.Balanced => Balanced,
        DefenseProfile.PhysicalTank => PhysicalTank,
        DefenseProfile.MagicalTank => MagicalTank,
        DefenseProfile.ElementalTank => ElementalTank,
        _ => Balanced
    };
}
