using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Interfaces.Abilities;
public interface ICondition
{
    bool IsSatisfied(CombatEntity source, CombatEntity target, ICombatContext combatContext);
    void PerformCondition(CombatEntity target);
    ICondition Clone();
}