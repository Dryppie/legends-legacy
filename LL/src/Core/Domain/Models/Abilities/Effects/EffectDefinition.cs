using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Damages;

namespace Domain.Models.Abilities.Effects;
public class EffectDefinition
{
    public IEffectAction Action { get; }
    public IEffectDuration Duration { get; }
    public IEffectInterval Interval { get; }
    public ICondition Condition { get; }
    public IUsage Usage { get; }
    public Targeting Targeting { get; }
    public TriggerEvent Trigger { get; }
    public Targeting TriggerTarget { get; }
    public bool IsFlatAmount { get; }
    public int Chance { get; }
    public List<EffectModification> EffectModifications { get; } = [];
    public EffectType EffectType => Action switch
    {
        ApplyStatusEffectAction => EffectType.StatusEffect,
        DamageAction => EffectType.Damage,
        ResourceRestoreAction => EffectType.Healing,
        ModifyAttributeAction => EffectType.ModifyAttribute,
        NestedEffectAction => EffectType.NestedEffect,
        SummonAction => EffectType.Summon,
        _ => throw new NotSupportedException($"Unsupported action type {Action?.GetType().Name}")
    };
    public AttackType AttackType { get; set; }
    public DamageType DamageType { get; set; }
    public List<EffectTag> EffectTags { get; set; } = [];
    public string Log { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;

    public EffectDefinition(IEffectAction action,
                  IEffectDuration duration,
                  ICondition condition,
                  IEffectInterval interval,
                  IUsage usage,
                  List<EffectTag> effectTags,
                  List<EffectModification> effectModifications,
                  Targeting targeting = Targeting.None,
                  TriggerEvent trigger = TriggerEvent.None,
                  Targeting triggerTarget = Targeting.None,
                  AttackType attackType = AttackType.None,
                  DamageType damageType = DamageType.None,
                  bool isFlatAmount = false,
                  int chance = 100)
    {
        Action = action;
        Duration = duration;
        EffectTags = effectTags;
        EffectModifications = effectModifications;
        Condition = condition;
        Interval = interval;
        Usage = usage;
        Targeting = targeting;
        Trigger = trigger;
        TriggerTarget = triggerTarget;
        IsFlatAmount = isFlatAmount;
        Chance = chance;
        AttackType = attackType;
        DamageType = damageType;
    }

    public EffectDefinition Clone()
    {
        var copy = new EffectDefinition(action: Action,
                            duration: Duration.Clone(),
                            condition: Condition.Clone(),
                            interval: Interval.Clone(),
                            usage: Usage, // Do not close Usage, as we need the reference to the original object. Otherwise Usage never reaches 0
                            targeting: Targeting,
                            trigger: Trigger,
                            triggerTarget: TriggerTarget,
                            isFlatAmount: IsFlatAmount,
                            chance: Chance,
                            effectModifications: EffectModifications,
                            effectTags: EffectTags,
                            attackType: AttackType,
                            damageType: DamageType);
        copy.Log = Log;
        copy.SourceId = SourceId;

        return copy;
    }
}