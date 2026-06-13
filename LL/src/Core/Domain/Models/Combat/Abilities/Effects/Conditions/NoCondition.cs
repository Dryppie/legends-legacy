using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Conditions;
// Effects default to this if they have no condition specified
public class NoCondition : ICondition
{
    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext) => true;
    public ICondition Clone() => new NoCondition();
    public void PerformCondition(CombatEntity target) {}
}
