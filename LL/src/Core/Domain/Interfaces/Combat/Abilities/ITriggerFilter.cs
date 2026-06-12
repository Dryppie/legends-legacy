using Domain.Models.Combat.Abilities;

namespace Domain.Interfaces.Combat.Abilities;
public interface ITriggerFilter
{
    bool IsMatch(CombatEvent e);
    ITriggerFilter Clone();
}
