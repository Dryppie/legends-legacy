using Domain.Interfaces.Combat;
using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public sealed class CombatantSummonedCondition : ICondition
{
    private readonly bool _useSource;

    public CombatantSummonedCondition(bool useSource)
    {
        _useSource = useSource;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var entity = _useSource ? source : target;
        return entity.IsSummoned;
    }

    public void PerformCondition(CombatEntity target)
    {
    }

    public ICondition Clone() => new CombatantSummonedCondition(_useSource);
}
