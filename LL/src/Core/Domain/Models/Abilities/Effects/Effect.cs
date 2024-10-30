using Domain.Interfaces;
using Domain.Models.Abilities.Effects.Timed;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Entities;

namespace Domain.Models.Abilities.Effects;
public class Effect
{
    public IEffectAction Action { get; }
    public IEffectDuration Duration { get; }
    public Targeting Targeting { get; }
    public TriggerEvent Trigger { get; }
    public IEffectInterval Interval { get; }
    public Entity? Caster { get; }
    public bool ApplyOnSelf { get; }
    public bool IsFlatAmount { get; }
    public string Log { get; set; } = string.Empty;

    public static event Action<EffectContext> OnEffectExecuted;

    public Effect(IEffectAction action,
                  IEffectDuration duration,
                  Entity? caster = null,
                  Targeting targeting = Targeting.None,
                  TriggerEvent trigger = TriggerEvent.None,
                  IEffectInterval? interval = null,
                  bool applyOnSelf = true,
                  bool isFlatAmount = false)
    {
        Action = action;
        Duration = duration;
        Caster = caster;
        Targeting = targeting;
        Trigger = trigger;
        Interval = interval ?? new NoInterval();
        ApplyOnSelf = applyOnSelf;
        IsFlatAmount = isFlatAmount;
    }

    public void Update()
    {
        Duration.DecrementDuration();
        Interval.Update();
    }

    public void ExecuteAction(EffectContext context)
    {
        Action?.Execute(context, OnEffectExecuted);
    }

    public void ExecuteOnExpireAction(EffectContext context)
    {
        Action?.OnExpireExecute(context, OnEffectExecuted);
    }

    public bool IsTrigger(TriggerEvent triggerEvent)
    {
        return Trigger == triggerEvent;
    }
}