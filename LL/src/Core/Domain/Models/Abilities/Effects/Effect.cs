using Domain.Interfaces;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Interval;
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
    public IEffectCondition Condition { get; }
    public Entity? Caster { get; }
    public bool ApplyOnSelf { get; }
    public bool IsFlatAmount { get; }
    public int Chance { get; }
    public List<EffectModification> EffectModifications { get; } = [];
    public List<string> Tags { get; } = []; // This could be a list of tags
                                            // Curse, Poison, Magic, Summon, Permanent, Fire, Lightning, Physical and so on
                                            // Unsure whether to keep
    public string Log { get; set; } = string.Empty;

    public static event Action<EffectContext> OnEffectExecuted;

    public Effect(IEffectAction action,
                  IEffectDuration duration,
                  IEffectCondition condition,
                  Entity? caster = null,
                  Targeting targeting = Targeting.None,
                  TriggerEvent trigger = TriggerEvent.None,
                  IEffectInterval? interval = null,
                  bool applyOnSelf = true,
                  bool isFlatAmount = false,
                  int chance = 100)
    {
        Action = action;
        Duration = duration;
        Condition = condition;
        Caster = caster;
        Targeting = targeting;
        Trigger = trigger;
        Interval = interval ?? new NoInterval();
        ApplyOnSelf = applyOnSelf;
        IsFlatAmount = isFlatAmount;
        Chance = chance;
    }

    public void Update()
    {
        Duration.DecrementDuration();
        Interval.Update();
    }

    public void ExecuteAction(EffectContext context)
    {
        if (!Condition.IsSatisfied(context)) return;

        if (Chance == 100 || Random.Shared.Next(1, 101) <= Chance)
        {
            // TODO: Apply EffectModifications properly. This might have to be checked during DamageCalculation and HealCalculation, and not here
            Action?.Execute(context, OnEffectExecuted);
        }
    }

    public void ExecuteOnExpireAction(EffectContext context)
    {
        if (Chance == 100 || Random.Shared.Next(1, 101) <= Chance)
        {
            Action?.OnExpireExecute(context, OnEffectExecuted);
        }
    }

    public bool IsTrigger(TriggerEvent triggerEvent)
    {
        return Trigger == triggerEvent;
    }
}