using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Conditions;
public class HealthCondition : ICondition
{
    public int HealthPercentage { get; }
    public ComparisonType Comparison { get; }

    public HealthCondition(int healthPercentage, ComparisonType comparison)
    {
        HealthPercentage = healthPercentage;
        Comparison = comparison;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var targetHealthPercent = (target.CombatAttributes[AttributeType.Health] / target.CombatAttributes[AttributeType.MaxHealth]) * 100;

        return Comparison switch
        {
            ComparisonType.LessThan => targetHealthPercent < HealthPercentage,
            ComparisonType.GreaterThan => targetHealthPercent >= HealthPercentage,
            ComparisonType.EqualTo => targetHealthPercent == HealthPercentage,
            _ => false,
        };
    }

    public ICondition Clone()
    {
        return new HealthCondition(HealthPercentage, Comparison);
    }

    public void PerformCondition(CombatEntity target) { }
}