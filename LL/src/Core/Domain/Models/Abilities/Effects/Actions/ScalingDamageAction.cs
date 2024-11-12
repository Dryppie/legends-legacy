using Domain.Interfaces;
using Domain.Models.Attributes;

namespace Domain.Models.Abilities.Effects.Actions;
public class ScalingDamageAction : IEffectAction
{
    private readonly int _damageAmount;
    public int Magnitude => _damageAmount;
    public AttributeType? DamageScalingAttribute {  get; set; }
    public float DamageScalingMultiplier { get; set; }
    public float ScalingFactor {  get; set; }
    public float Interval {  get; set; }
    public AttributeType ScalingAttribute { get; set; }

    public ScalingDamageAction(int damageAmount, float scalingFactor, float interval, AttributeType scalingAttribute)
    {
        _damageAmount = damageAmount;
        ScalingFactor = scalingFactor;
        Interval = interval;
        ScalingAttribute = scalingAttribute;
    }

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        // apply the scalingFactor to Magnitude, in combination with the interval, based on the scalingAttribute
        // +2% damage per 5% health lost
        // This might have to be an IScalingEffect, as there could be scaling to buffs, debuffs, heals, summons, and so on
    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        
    }
}
