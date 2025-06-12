using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Conditions;
// Effects default to this if they have no condition specified
public class NoCondition : ICondition
{
    public bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext) => true;
    public ICondition Clone() => new NoCondition();
    public void PerformCondition(CombatEntity target) {}
}
