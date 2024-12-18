using Domain.Helpers;
using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Domain.Models.Abilities.Effects.Actions;
public class DamageAction : IEffectAction
{
    private readonly int _damageAmount;
    public int Magnitude => _damageAmount;
    public AttackType AttackType { get; }
    public DamageType DamageType { get; }
    public List<DamageTag> DamageTags { get; } = [];
    public AttributeType? DamageScalingAttribute { get; set; }
    public float DamageScalingMultiplier { get; set; }

    public DamageAction(int damageAmount, AttributeType? damageScalingAttribute, float damageScalingMultiplier, AttackType attackType, DamageType damageType, List<DamageTag> damageTags)
    {
        _damageAmount = damageAmount;
        DamageScalingAttribute = damageScalingAttribute;
        DamageScalingMultiplier = damageScalingMultiplier;
        AttackType = attackType;
        DamageType = damageType;
        DamageTags = damageTags;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var calculatedResult = new CalculatedResult();
        if (context.IsFlatAmount)
        {
            // If it's a flat amount, take the value of the effect itself (_damageAmount)
            calculatedResult.CalculatedDamageToDeal = Magnitude;
            calculatedResult.CalculatedDamageReceived = combatContext.InteractionManager.CalculateDamageReceived(context.Target, Magnitude, AttackOutcome.Hit);
        }
        else
        {
            calculatedResult = CombatFormulaCalculator.CalculateCombatInteraction(context.Owner, context.Target, context.Magnitude);
        }

        context.Magnitude = calculatedResult.CalculatedDamageReceived;
        context.EventType = EventType.Damage;
        context.AttackOutcome = calculatedResult.AttackOutcome;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", calculatedResult.CalculatedDamageReceived.ToString());

        combatContext.LogEffectExecution(context);
        Console.WriteLine(context.Details);
        combatContext.InteractionManager.ApplyDamage(context.Owner, context.Target, context.Magnitude);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}