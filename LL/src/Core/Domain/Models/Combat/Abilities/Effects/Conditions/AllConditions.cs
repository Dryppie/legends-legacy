using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;

public class AllConditions : ICondition
{
    private readonly IReadOnlyList<ICondition> _conditions;

    public AllConditions(IReadOnlyList<ICondition> conditions)
    {
        _conditions = conditions;
    }

    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext) =>
        _conditions.All(condition => condition.IsSatisfied(source, target, combatContext));

    public void PerformCondition(CombatEntity target)
    {
        foreach (var condition in _conditions)
            condition.PerformCondition(target);
    }

    public ICondition Clone() => new AllConditions([.. _conditions.Select(x => x.Clone())]);
}
