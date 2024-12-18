using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Interval;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;
using Domain.Models.Damages;
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
    public EffectType EffectType => Action switch
    {
        ApplyStatusEffectAction => EffectType.StatusEffect,
        DamageAction => EffectType.Damage,
        HealingAction => EffectType.Healing,
        ModifyAttributeAction => EffectType.ModifyAttribute,
        SummonAction => EffectType.Summon,
        _ => throw new NotSupportedException($"Unsupported action type {Action?.GetType().Name}")
    };
    public AttackType AttackType { get; set; }
    public DamageType DamageType { get; set; }
    public List<EffectTag> EffectTags { get; set; } = [];

    public string Log { get; set; } = string.Empty;

    public Effect(IEffectAction action,
                  IEffectDuration duration,
                  List<EffectTag> effectTags,
                  IEffectCondition? condition = null,
                  Entity? caster = null,
                  Targeting targeting = Targeting.None,
                  TriggerEvent trigger = TriggerEvent.None,
                  IEffectInterval? interval = null,
                  bool applyOnSelf = true,
                  bool isFlatAmount = false,
                  int chance = 100,
                  AttackType attackType = AttackType.None,
                  DamageType damageType = DamageType.None)
    {
        Action = action;
        Duration = duration;
        EffectTags = effectTags;
        Condition = condition ?? new NoCondition();
        Caster = caster;
        Targeting = targeting;
        Trigger = trigger;
        Interval = interval ?? new NoInterval();
        ApplyOnSelf = applyOnSelf;
        IsFlatAmount = isFlatAmount;
        Chance = chance;
        AttackType = attackType;
        DamageType = damageType;
    }

    public void Update()
    {
        Duration.DecrementDuration();
        Interval.Update();
    }

    public void ExecuteAction(EffectContext context, ICombatContext combatContext)
    {
        if (!Condition.IsSatisfied(context)) return;

        if (Chance == 100 || Random.Shared.Next(1, 101) <= Chance)
        {
            // TODO: Apply EffectModifications properly. This might have to be checked during DamageCalculation and HealCalculation, and not here
            Action?.Execute(context, combatContext);
        }
    }

    public void ExecuteOnExpireAction(EffectContext context, ICombatContext combatContext)
    {
        if (Chance == 100 || Random.Shared.Next(1, 101) <= Chance)
        {
            Action?.OnExpireExecute(context,combatContext);
        }
    }

    public bool IsTrigger(TriggerEvent triggerEvent)
    {
        return Trigger == triggerEvent;
    }
}