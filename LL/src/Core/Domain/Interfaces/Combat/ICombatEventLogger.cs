using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatEventLogger
{
    void LogEvent(CombatEvent combatEvent);
}