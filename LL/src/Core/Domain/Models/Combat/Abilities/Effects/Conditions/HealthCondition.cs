using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;
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
        var maxHealth = target.GetAttributeValue(AttributeType.MaxHealth);
        if (maxHealth <= 0)
            return false;

        var targetHealthPercent = (target.CurrentHealth / maxHealth) * 100;

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
