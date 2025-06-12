using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
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
            var allTriggers = entity.Abilities
                .SelectMany(a => a.Definition.Triggers)
                .Concat(entity.Statuses.SelectMany(s => s.Definition.Triggers));

            foreach (var trigger in allTriggers)
            {
                if (trigger.Event != e.Type)
                    continue;

                if (trigger.Filters.Count > 0 && !trigger.Filters.All(f => f.IsMatch(e)))
                    continue;

                foreach (var effect in trigger.Actions)
                {
                    var targets = new List<CombatEntity>();

                    var triggerTarget = effect.Targeting;

                    if (triggerTarget.Equals(Targeting.CauseOfTrigger))
                    {
                        targets.Add(e.Target!);
                    }
                    //else if (triggerTarget.Equals(Targeting.AttackedEnemy))
                    //{
                    //    targets.Add(causeOfTrigger!);
                    //}
                    else
                    {
                        targets = _context.EntityManager.SelectTargets(entity, effect.Targeting);
                    }
                    foreach (var target in targets)
                    {
                        var instance = new EffectInstance(effect.Clone(), entity, target);
                        _effectManager.AddEffect(instance);
                    }
                }
            }
        }
    }

    public void Dispose() => _bus.Unsubscribe(HandleEvent);
}
