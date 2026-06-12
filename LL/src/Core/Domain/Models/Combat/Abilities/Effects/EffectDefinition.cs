using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.EffectModifications;
using Domain.Models.Damages;

namespace Domain.Models.Combat.Abilities.Effects;
public class EffectDefinition
{
    public IEffectAction Action { get; }
    public IEffectDuration Duration { get; }
    public IEffectInterval Interval { get; }
    public ICondition Condition { get; }
    public IUsage Usage { get; }
    public CombatTargeting Targeting { get; }
    public int Chance { get; }
    public List<EffectModification> EffectModifications { get; } = [];
    public EffectType EffectType => Action switch
    {
        ApplyStatusAction => EffectType.NestedEffect,
        ApplyStatusEffectAction => EffectType.StatusEffect,
        RemoveStatusAction => EffectType.StatusEffect,
        CleanseAction => EffectType.StatusEffect,
        TriggerSecondaryEffectAction => EffectType.NestedEffect,
        DamageAction => EffectType.Damage,
        ResourceRestoreAction => EffectType.Healing,
        ModifyAttributeAction => EffectType.ModifyAttribute,
        SummonAction => EffectType.Summon,
        _ => throw new NotSupportedException($"Unsupported action type {Action?.GetType().Name}")
    };
    public AttackType AttackType { get; set; }
    public DamageType DamageType { get; set; }
    public List<EffectTag> EffectTags { get; set; } = [];
    public string Log { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;

    public EffectDefinition(IEffectAction action,
                  IEffectDuration duration,
                  ICondition condition,
                  IEffectInterval interval,
                  IUsage usage,
                  List<EffectTag> effectTags,
                  List<EffectModification> effectModifications,
                  CombatTargeting targeting = CombatTargeting.None,
                  AttackType attackType = AttackType.None,
                  DamageType damageType = DamageType.None,
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
                            chance: Chance,
                            effectModifications: EffectModifications,
                            effectTags: EffectTags,
                            attackType: AttackType,
                            damageType: DamageType);
        copy.Log = Log;
        copy.SourceName = SourceName;

        return copy;
    }
}