using Domain.Models.Combat;

namespace Domain.Interfaces;
public interface ICombatEventLogger
{
    void LogEvent(CombatEvent combatEvent);
}