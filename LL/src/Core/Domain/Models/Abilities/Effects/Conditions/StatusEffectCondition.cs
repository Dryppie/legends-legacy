using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.StatusEffects;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Conditions;
public class StatusEffectCondition : ICondition
{
    private StatusEffectType _type { get; set; }
    private int _stacksRequired { get; }
    private ComparisonType _comparison { get; }

    public StatusEffectCondition(StatusEffectType type, int stacksRequired, ComparisonType comparison)
    {
        _type = type;
        _stacksRequired = stacksRequired;
        _comparison = comparison;
    }

    public ICondition Clone()
    {
        return new StatusEffectCondition(_type, _stacksRequired, _comparison);
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        if (target.StatusEffects.TryGetValue(_type, out int value))
        {
            var comparisonFulfilled = _comparison switch
            {
                ComparisonType.LessThan => _stacksRequired > value,
                ComparisonType.GreaterThan => _stacksRequired <= value,
                ComparisonType.EqualTo => _stacksRequired == value,
                _ => false,
            };
            if (comparisonFulfilled)
            {
                PerformCondition(target);
                return true;
            }
        }
        return false;
    }

    public void PerformCondition(CombatEntity target)
    {
        target.ModifyStatusEffects(_type, -_stacksRequired);
    }
}
