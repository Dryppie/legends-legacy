using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.CombatEngine;
public class CombatEventBus : ICombatEventBus
{
    private readonly List<Action<CombatEvent>> _subscribers = new();

    public void Subscribe(Action<CombatEvent> callback)
    {
        _subscribers.Add(callback);
    }

    public void Publish(CombatEvent e)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.Invoke(e);
        }
    }

    public void Unsubscribe(Action<CombatEvent> callback)
    {
        _subscribers.Remove(callback);
    }
}