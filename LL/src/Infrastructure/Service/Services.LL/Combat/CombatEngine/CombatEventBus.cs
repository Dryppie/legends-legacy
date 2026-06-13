using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.CombatEngine;
public class CombatEventBus : ICombatEventBus
{
    private const int MaxPublishDepth = 16;
    private readonly List<Action<CombatEvent>> _subscribers = new();
    private int _publishDepth;

    public void Subscribe(Action<CombatEvent> callback)
    {
        _subscribers.Add(callback);
    }

    public void Publish(CombatEvent e)
    {
        if (_publishDepth >= MaxPublishDepth)
            throw new InvalidOperationException($"Combat event dispatch exceeded maximum depth of {MaxPublishDepth}. Check triggered effects for recursion.");

        _publishDepth++;
        try
        {
            foreach (var subscriber in _subscribers.ToList())
            {
                subscriber.Invoke(e);
            }
        }
        finally
        {
            _publishDepth--;
        }
    }

    public void Unsubscribe(Action<CombatEvent> callback)
    {
        _subscribers.Remove(callback);
    }
}
