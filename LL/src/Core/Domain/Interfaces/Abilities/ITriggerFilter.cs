using Domain.Models.Abilities;

namespace Domain.Interfaces.Abilities;
public interface ITriggerFilter
{
    bool IsMatch(CombatEvent e);
    ITriggerFilter Clone();
}
