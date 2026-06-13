using Domain.Models.Combat.Abilities;

namespace Domain.Interfaces.Combat;
public interface ICombatEventBus
{
    void Subscribe(Action<CombatEvent> callback);
    void Unsubscribe(Action<CombatEvent> callback);
    void Publish(CombatEvent e);
}
