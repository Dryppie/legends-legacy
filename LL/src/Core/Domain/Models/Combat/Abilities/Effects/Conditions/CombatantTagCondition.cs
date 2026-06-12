using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public class CombatantTagCondition : ICondition
{
    private readonly bool _useSource;
    private readonly string _tag;

    public CombatantTagCondition(bool useSource, string tag)
    {
        _useSource = useSource;
        _tag = tag;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext)
    {
        var entity = _useSource ? source : target;
        return entity.Tags.Contains(_tag);
    }

    public void PerformCondition(CombatEntity target)
    {
    }

    public ICondition Clone() => new CombatantTagCondition(_useSource, _tag);
}
