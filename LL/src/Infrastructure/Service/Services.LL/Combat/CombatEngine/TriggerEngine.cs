using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Triggers;
using Domain.Models.Combat;

namespace Services.LL.Combat.CombatEngine;
public class TriggerEngine : IDisposable
{
    private readonly ICombatContext _context;
    private readonly ICombatEventBus _bus;
    private readonly ICombatEffectManager _effectManager;

    public TriggerEngine(ICombatContext context, ICombatEventBus bus, ICombatEffectManager effectManager)
    {
        _context = context;
        _bus = bus;
        _effectManager = effectManager;
    }

    public void Initialize()
    {
        _bus.Subscribe(HandleEvent);
    }

    private void HandleEvent(CombatEvent e)
    {
        foreach (var entity in _context.EntityManager.AllEntities)
        {
            // 🔹 1. First: Process ability triggers (no mutable state)
            foreach (var ability in entity.Abilities)
            {
                foreach (var trigger in ability.Definition.Triggers)
                {
                    if (!ShouldTrigger(trigger, e))
                        continue;

                    ExecuteTriggerActions(trigger, entity, e);
                }
            }

            foreach (var status in entity.Statuses.ToList()) // Clone for safe mutation
            {
                foreach (var trigger in status.Definition.Triggers)
                {
                    if (!ShouldTrigger(trigger, e))
                        continue;
                    if (!status.CanUse())
                        continue;

                    ExecuteTriggerActions(trigger, entity, e);
                    status.ConsumeUse();
                }
            }
        }
    }

    private bool ShouldTrigger(Trigger trigger, CombatEvent e)
    {
        if (trigger.Event != e.Type)
            return false;

        return trigger.Filters.Count == 0 || trigger.Filters.All(f => f.IsMatch(e));
    }

    private void ExecuteTriggerActions(Trigger trigger, CombatEntity source, CombatEvent e)
    {
        foreach (var effect in trigger.Actions)
        {
            var targets = effect.Targeting == Targeting.CauseOfTrigger
                ? new List<CombatEntity> { e.Target! }
                : _context.EntityManager.SelectTargets(source, effect.Targeting);

            foreach (var target in targets)
            {
                var instance = new EffectInstance(effect.Clone(), source, target);
                _effectManager.AddEffect(instance);
            }
        }
    }

    public void Dispose() => _bus.Unsubscribe(HandleEvent);
}
