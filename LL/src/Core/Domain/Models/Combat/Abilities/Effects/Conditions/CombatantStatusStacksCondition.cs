using Domain.Interfaces.Combat;
using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public sealed class CombatantStatusStacksCondition : ICondition
{
    private readonly bool _useSource;
    private readonly StatusEffectType _statusEffect;
    private readonly int _stacksRequired;

    public CombatantStatusStacksCondition(bool useSource, StatusEffectType statusEffect, int stacksRequired)
    {
        _useSource = useSource;
        _statusEffect = statusEffect;
        _stacksRequired = stacksRequired;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var entity = _useSource ? source : target;
        return entity.StatusEffects.TryGetValue(_statusEffect, out var stacks) && stacks >= _stacksRequired;
    }

    public void PerformCondition(CombatEntity target)
    {
    }

    public ICondition Clone() => new CombatantStatusStacksCondition(_useSource, _statusEffect, _stacksRequired);
}
