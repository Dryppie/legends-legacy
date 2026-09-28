namespace Domain.Models.Combat.Abilities;

public enum ConditionResistancePolicy { Unaffected, Application }

/// <summary>Semantic resistance policy shared by standard conditions, UI and simulation.</summary>
public static class ConditionResistanceRules
{
    public static ConditionResistancePolicy For(StandardConditionType condition) => condition switch
    {
        StandardConditionType.Slow or StandardConditionType.Weaken or StandardConditionType.Wound
            or StandardConditionType.Decay or StandardConditionType.Poison or StandardConditionType.Burn
            or StandardConditionType.Bleed or StandardConditionType.Stun or StandardConditionType.Chill
            or StandardConditionType.Freeze or StandardConditionType.Corrosion or StandardConditionType.Mark
            or StandardConditionType.Silence or StandardConditionType.Exposed or StandardConditionType.Doom
            or StandardConditionType.Vulnerable or StandardConditionType.Soaked => ConditionResistancePolicy.Application,
        // Beneficial effects and authored attention semantics. Boss Stagger is resolved separately.
        _ => ConditionResistancePolicy.Unaffected
    };

    public static bool IsHarmfulStatus(IEnumerable<string> tags) => tags.Any(tag =>
        tag.StartsWith("Control.", StringComparison.OrdinalIgnoreCase)
        || tag.StartsWith("Debuff", StringComparison.OrdinalIgnoreCase)
        || tag.StartsWith("Affliction", StringComparison.OrdinalIgnoreCase)
        || tag.StartsWith("Status.Debuff", StringComparison.OrdinalIgnoreCase)
        || tag.StartsWith("Status.Affliction", StringComparison.OrdinalIgnoreCase));
}
