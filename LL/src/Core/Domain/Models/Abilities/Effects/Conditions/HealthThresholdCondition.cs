using Domain.Interfaces;
using Domain.Models.Attributes;

namespace Domain.Models.Abilities.Effects.Conditions;
public class HealthThresholdCondition : IEffectCondition
{
    public int HealthPercentage { get; }
    public ComparisonType Comparison { get; }

    public HealthThresholdCondition(int healthPercentage, ComparisonType comparison)
    {
        HealthPercentage = healthPercentage;
        Comparison = comparison;
    }

    public bool IsSatisfied(EffectContext context)
    {
        var targetHealthPercent = (context.Target.CombatAttributes[AttributeType.Health] / context.Target.CombatAttributes[AttributeType.MaxHealth]) * 100;

        return Comparison switch
        {
            ComparisonType.LessThan => targetHealthPercent < HealthPercentage,
            ComparisonType.GreaterThan => targetHealthPercent >= HealthPercentage,
            ComparisonType.EqualTo => targetHealthPercent == HealthPercentage,
            _ => false,
        };
    }
}