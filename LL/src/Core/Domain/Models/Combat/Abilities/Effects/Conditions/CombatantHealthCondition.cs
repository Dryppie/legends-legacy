using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public class CombatantHealthCondition : ICondition
{
    private readonly bool _useSource;
    private readonly int _healthPercentage;
    private readonly ComparisonType _comparison;

    public CombatantHealthCondition(bool useSource, int healthPercentage, ComparisonType comparison)
    {
        _useSource = useSource;
        _healthPercentage = healthPercentage;
        _comparison = comparison;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var entity = _useSource ? source : target;
        var maxHealth = entity.GetAttributeValue(AttributeType.MaxHealth);
        if (maxHealth <= 0)
            return false;

        var targetHealthPercent = (entity.CurrentHealth / maxHealth) * 100;

        return _comparison switch
        {
            ComparisonType.LessThan => targetHealthPercent < _healthPercentage,
            ComparisonType.GreaterThan => targetHealthPercent >= _healthPercentage,
            ComparisonType.EqualTo => targetHealthPercent == _healthPercentage,
            _ => false
        };
    }

    public void PerformCondition(CombatEntity target)
    {
    }

    public ICondition Clone() => new CombatantHealthCondition(_useSource, _healthPercentage, _comparison);
}
